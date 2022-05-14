﻿using HealthInstitution.Core.Examinations.Model;
using HealthInstitution.Core.Examinations.Repository;
using HealthInstitution.Core.SystemUsers.Doctors.Model;
using HealthInstitution.Core.SystemUsers.Doctors.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace HealthInstitution.GUI.DoctorView
{
    /// <summary>
    /// Interaction logic for ExaminationTable.xaml
    /// </summary>
    public partial class ExaminationTable : Window
    {
        private ExaminationRepository _examinationRepository = ExaminationRepository.GetInstance();
        private DoctorRepository _doctorRepository = DoctorRepository.GetInstance();
        private ExaminationDoctorRepository _examinationDoctorRepository = ExaminationDoctorRepository.GetInstance();
        private Doctor _loggedDoctor;
        public ExaminationTable(Doctor doctor)
        {
            this._loggedDoctor = doctor;
            InitializeComponent();
            LoadRows();
        }
        
        private void LoadRows()
        {
            dataGrid.Items.Clear();
            List<Examination> doctorExaminations = this._loggedDoctor.Examinations;
            foreach (Examination examination in doctorExaminations)
            {
                dataGrid.Items.Add(examination);
            }
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            new AddExaminationDialog(this._loggedDoctor).ShowDialog();
            LoadRows();
            dataGrid.Items.Refresh();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Examination selectedExamination = (Examination)dataGrid.SelectedItem;
            EditExaminationDialog editExaminationDialog = new EditExaminationDialog(selectedExamination);
            editExaminationDialog.ShowDialog();
            LoadRows();
            dataGrid.Items.Refresh();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var answer = System.Windows.MessageBox.Show("Are you sure you want to delete selected examination", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer == MessageBoxResult.Yes)
            {
                Examination selectedExamination = (Examination)dataGrid.SelectedItem;
                dataGrid.Items.Remove(selectedExamination);
                _examinationRepository.Delete(selectedExamination.Id);
                _doctorRepository.DeleteExamination(_loggedDoctor, selectedExamination);
                _examinationDoctorRepository.Save();
            }
        }
    }
}