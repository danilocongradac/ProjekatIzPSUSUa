
﻿using DataConcentrator;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ScadaGUI
{
    public partial class AddTagWindow : Window
    {
        public Tag CreatedTag { get; private set; }

        public AddTagWindow()
        {
            InitializeComponent();
        }

        private void cmbTagType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedType = ((ComboBoxItem)cmbTagType.SelectedItem)?.Content.ToString();

            // Hide all panels
            panelInputProps.Visibility = Visibility.Collapsed;
            panelAnalogProps.Visibility = Visibility.Collapsed;
            panelOutputProps.Visibility = Visibility.Collapsed;

            if (selectedType == "DI" || selectedType == "AI")
                panelInputProps.Visibility = Visibility.Visible;

            if (selectedType == "AI")
                panelAnalogProps.Visibility = Visibility.Visible;

            if (selectedType == "DO" || selectedType == "AO")
                panelOutputProps.Visibility = Visibility.Visible;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tag newTag = new Tag
                {
                    Name = txtName.Text,
                    Description = txtDescription.Text,
                    IOAddress = txtIOAddress.Text,
                    Type = (TagType)Enum.Parse(typeof(TagType), ((ComboBoxItem)cmbTagType.SelectedItem).Content.ToString()),
                    Alarms = new List<Alarm>(),
                    ExtraProperties = new Dictionary<TagProperty, object>()
                };

                // Extra props for input tags
                if (newTag.Type == TagType.DI || newTag.Type == TagType.AI)
                {
                    if (!string.IsNullOrWhiteSpace(txtScanTime.Text))
                        newTag.ExtraProperties[DataConcentrator.TagProperty.scantime] = int.Parse(txtScanTime.Text);
                    newTag.ExtraProperties[DataConcentrator.TagProperty.onoffscan] = chkOnOffScan.IsChecked == true;
                }

                // Extra props for analog tags
                if (newTag.Type == TagType.AI)
                {
                    if (!string.IsNullOrWhiteSpace(txtLowLimit.Text))
                        newTag.ExtraProperties[DataConcentrator.TagProperty.lowlimit] = double.Parse(txtLowLimit.Text);
                    if (!string.IsNullOrWhiteSpace(txtHighLimit.Text))
                        newTag.ExtraProperties[DataConcentrator.TagProperty.highlimit] = double.Parse(txtHighLimit.Text);
                    newTag.ExtraProperties[DataConcentrator.TagProperty.units] = txtUnits.Text;
                }

                // Extra props for output tags
                if (newTag.Type == TagType.DO || newTag.Type == TagType.AO)
                {
                    if (!string.IsNullOrWhiteSpace(txtInitialValue.Text))
                    {
                        newTag.ExtraProperties[DataConcentrator.TagProperty.initialvalue] = double.Parse(txtInitialValue.Text);
                        if (newTag.Type == TagType.DO && !(Convert.ToDouble(txtInitialValue.Text) == 0 || Convert.ToDouble(txtInitialValue.Text) == 1))
                        {
                            newTag.Value = 0;
                            newTag.ExtraProperties[DataConcentrator.TagProperty.initialvalue] = 0;
                            MessageBox.Show("The value of DO tag can be 0 or 1, set to 0");
                        }
                        else
                        {
                            newTag.Value = Convert.ToDouble(txtInitialValue.Text);
                        }
                    }


                }

                // save to db
                using (var db = new ContextClass())
                {
                    db.Tags.Add(newTag);
                    db.SaveChanges();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}