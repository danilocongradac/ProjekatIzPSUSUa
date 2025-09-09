
﻿using DataConcentrator;
using System;
using System.Linq;
using System.Windows;


namespace ScadaGUI
{
    public partial class MainWindow : Window
    {
        private Tag selectedTag;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDataBase();
            LoadTagsFromDatabase();
        }

        private void InitializeDataBase()
        {
            using (var db = new ContextClass())
            {
                db.Database.CreateIfNotExists();
            }
        }


        private void LoadTagsFromDatabase()
        {
            using (var db = new ContextClass())
            {

                // sort: DI, DO, AI, AO
                var sortedTags = db.Tags.ToList()
                    .OrderBy(t => t.Type == TagType.DI ? 0 :
                                  t.Type == TagType.DO ? 1 :
                                  t.Type == TagType.AI ? 2 : 3)
                    .ToList();

                dgTags.ItemsSource = sortedTags;
            }
        }

        private void btnAddTag_Click(object sender, RoutedEventArgs e)
        {
           
            // opening AddTagWindow
            AddTagWindow addWindow = new AddTagWindow();
            if (addWindow.ShowDialog() == true)
            {
                
                LoadTagsFromDatabase();
                MessageBox.Show("Successfully added Tag!");
            }

        }


        private void btnDeleteTag_Click(object sender, RoutedEventArgs e)
        {

            selectedTag = dgTags.SelectedItem as Tag;
            if (selectedTag == null)
            {
                MessageBox.Show("You didn't select a Tag!");
                return;
            }
            
            try
            {
               
                using (var db = new ContextClass())
                {
                    db.Tags.Attach(selectedTag);
                    db.Tags.Remove(selectedTag);
                    db.SaveChanges();
                }

                LoadTagsFromDatabase();
                selectedTag = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void dgTags_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Tag tag = dgTags.SelectedItem as Tag;
            if (tag == null) return;

            TagDetailsWindow detailsWindow = new TagDetailsWindow(tag);
            detailsWindow.ShowDialog();
   
            LoadTagsFromDatabase();
        }
    }
}