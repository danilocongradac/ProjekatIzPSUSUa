
﻿using DataConcentrator;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Linq;


namespace ScadaGUI
{
    public partial class TagDetailsWindow : Window
    {
        private Tag selectedTag;

        public TagDetailsWindow(Tag tag)
        {
            InitializeComponent();

            using (var db = new ContextClass())
            {
                selectedTag = db.Tags
                                .Include(t => t.Alarms)
                                .FirstOrDefault(t => t.Id == tag.Id);
            }

            if (selectedTag == null)
            {
                MessageBox.Show("Tag could not be found in db!");
                Close();
                return;
            }

            txtTagInfo.Text = $"Tag: {selectedTag.Name} ({selectedTag.Type}) - {selectedTag.Description}, IO: {selectedTag.IOAddress}, Value: {selectedTag.Value}";

            LoadExtraProps();

            if (selectedTag.Type == TagType.AI)
                panelAlarms.Visibility = Visibility.Visible;
            else
                panelAlarms.Visibility = Visibility.Collapsed;

            lstAlarms.Items.Clear();
            if (selectedTag.Alarms != null)
            {
                foreach (var alarm in selectedTag.Alarms)
                {
                    lstAlarms.Items.Add(alarm.ToString());
                }
            }
        }
        private void LoadExtraProps()
        {
            panelInputProps.Visibility = Visibility.Collapsed;
            panelAnalogProps.Visibility = Visibility.Collapsed;
            panelOutputProps.Visibility = Visibility.Collapsed;

            if (selectedTag.Type == TagType.DI || selectedTag.Type == TagType.AI)
            {
                panelInputProps.Visibility = Visibility.Visible;

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.scantime, out object st))
                    txtScanTime.Text = GetValueAsString(st);

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.onoffscan, out object onoff))
                    chkOnOffScan.IsChecked = GetValueAsBool(onoff);
            }

            if (selectedTag.Type == TagType.AI || selectedTag.Type == TagType.AO)
            {
                panelAnalogProps.Visibility = Visibility.Visible;

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.lowlimit, out object ll))
                    txtLowLimit.Text = GetValueAsString(ll);

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.highlimit, out object hl))
                    txtHighLimit.Text = GetValueAsString(hl);

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.units, out object u))
                    txtUnits.Text = GetValueAsString(u);
            }

            if (selectedTag.Type == TagType.DO || selectedTag.Type == TagType.AO)
            {
                panelOutputProps.Visibility = Visibility.Visible;

                if (selectedTag.ExtraProperties.TryGetValue(DataConcentrator.TagProperty.initialvalue, out object iv))
                    txtInitialValue.Text = GetValueAsString(iv);
            }
        }

        // Helper 
        private string GetValueAsString(object value)
        {
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number) return je.GetDouble().ToString();
                if (je.ValueKind == JsonValueKind.String) return je.GetString();
                if (je.ValueKind == JsonValueKind.True) return "True";
                if (je.ValueKind == JsonValueKind.False) return "False";
            }
            return value?.ToString() ?? "";
        }

        private bool? GetValueAsBool(object value)
        {
            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.True) return true;
                if (je.ValueKind == JsonValueKind.False) return false;
            }
            if (value is bool b) return b;
            return null;
        }


        // Event handler for text cell
        private void txtExtraProp_LostFocus(object sender, RoutedEventArgs e)
        {
            if (selectedTag == null) return;

            try
            {
                if (sender == txtScanTime && double.TryParse(txtScanTime.Text, out double st))
                    selectedTag.AddProperty(DataConcentrator.TagProperty.scantime, st);
                else if (sender == txtLowLimit && double.TryParse(txtLowLimit.Text, out double ll))
                    selectedTag.AddProperty(DataConcentrator.TagProperty.lowlimit, ll);
                else if (sender == txtHighLimit && double.TryParse(txtHighLimit.Text, out double hl))
                    selectedTag.AddProperty(DataConcentrator.TagProperty.highlimit, hl);
                else if (sender == txtUnits)
                    selectedTag.AddProperty(DataConcentrator.TagProperty.units, txtUnits.Text);
                else if (sender == txtInitialValue && double.TryParse(txtInitialValue.Text, out double iv))
                    selectedTag.AddProperty(DataConcentrator.TagProperty.initialvalue, iv); // value is immediately updated
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }

            updateTagInDB(selectedTag);
        }

        // Event handler for checkbox
        private void chkExtraProp_Changed(object sender, RoutedEventArgs e)
        {
            if (selectedTag == null) return;
            selectedTag.AddProperty(DataConcentrator.TagProperty.onoffscan, chkOnOffScan.IsChecked == true);
            updateTagInDB(selectedTag);
        }


        private void updateTagInDB(Tag tag)
        {
            using (var db = new ContextClass())
            {
                db.Tags.AddOrUpdate(tag);
                db.SaveChanges();
            }
        }

        private void btnWriteValue_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTag == null)
            {
                MessageBox.Show("Tag isn't selected!");
                return;
            }

            if (selectedTag.Type == TagType.DI || selectedTag.Type == TagType.AI)
            {
                MessageBox.Show("You can't write into input tags!");
                return;
            }

            object value = txtNewValue.Text;
            if (!double.TryParse(Convert.ToString(value), out double dvalue))
            {
                MessageBox.Show("Error: input value isn't a number!");
                return;
            }

            if (selectedTag.Type == TagType.DO && !(dvalue == 0 || dvalue == 1))
            {
                MessageBox.Show("Wrong input value for DO tag (allowed 0 or 1)");
                return;
            }

            selectedTag.WriteValue(value);
            updateTagInDB(selectedTag);

            MessageBox.Show($"The value for tag {selectedTag.Name} is updated on {value}");
            txtTagInfo.Text = $"Tag: {selectedTag.Name} ({selectedTag.Type}) - {selectedTag.Description}, IO: {selectedTag.IOAddress}, Value: {selectedTag.Value}";
        }

        private void btnAddAlarm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedTag == null)
                {
                    MessageBox.Show("Tag isn't selected!");
                    return;
                }


                double limit = Convert.ToDouble(txtAlarmLimit.Text);
                AlarmType type = (AlarmType)Enum.Parse(typeof(AlarmType), ((ComboBoxItem)cbAlarmType.SelectedItem).Content.ToString());
                string message = txtAlarmMessage.Text;

                Alarm alarm = new Alarm(limit, type, message)
                {
                    TagId = selectedTag.Id
                };

                using (var db = new ContextClass())
                {
                    db.Alarms.Add(alarm);
                    db.SaveChanges();
                }

                // Add a list for GUI
                if (selectedTag.Alarms == null)
                    selectedTag.Alarms = new List<Alarm>();

                selectedTag.Alarms.Add(alarm);
                lstAlarms.Items.Add(alarm.ToString());

                MessageBox.Show("Alarm successfully added!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

    }
}