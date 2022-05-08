﻿using HealthInstitution.Core.SystemUsers.Patients.Model;
using HealthInstitution.Core.SystemUsers.Patients.Repository;
using HealthInstitution.Core.SystemUsers.Users.Repository;
using HealthInstitution.GUI.SecretaryView;
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

namespace HealthInstitution.GUI.UserWindow
{
    /// <summary>
    /// Interaction logic for PatientsTable.xaml
    /// </summary>
    public partial class PatientsTable : Window
    {
        public PatientsTable()
        {
            InitializeComponent();
            loadRows();
        }
        private void loadRows()
        {
            dataGrid.Items.Clear();
            List<Patient> patients = PatientRepository.GetInstance().Patients;
            foreach (Patient patient in patients)
            {
                dataGrid.Items.Add(patient);
            }
            dataGrid.Items.Refresh();
        }
        private void createPatient_Click(object sender, RoutedEventArgs e)
        {
            CreatePatientDialog createPatientDialog = new CreatePatientDialog();
            createPatientDialog.ShowDialog();
            loadRows();
        }

        private void updatePatient_Click(object sender, RoutedEventArgs e)
        {
            Patient selectedPatient = (Patient)dataGrid.SelectedItem;
            if (selectedPatient != null) 
            {
                UpdatePatientWindow updatePatientWindow = new UpdatePatientWindow(selectedPatient);
                updatePatientWindow.ShowDialog();
                dataGrid.SelectedItem = null;
                loadRows();
                
            }
        }

        private void deletePatient_Click(object sender, RoutedEventArgs e)
        {
            Patient selectedPatient = (Patient)dataGrid.SelectedItem;
            if (selectedPatient != null)
            {
                UserRepository userRepository = UserRepository.GetInstance();
                PatientRepository patientRepository = PatientRepository.GetInstance();
                patientRepository.Delete(selectedPatient.Username);
                userRepository.Delete(selectedPatient.Username);
                dataGrid.SelectedItem = null;
                loadRows();
            }
        }

        private void blockPatient_Click(object sender, RoutedEventArgs e)
        {
            Patient selectedPatient = (Patient)dataGrid.SelectedItem;
            if (selectedPatient != null)
            {
                PatientRepository patientRepository = PatientRepository.GetInstance();
                patientRepository.ChangeBlockedStatus(selectedPatient.Username);
                dataGrid.SelectedItem = null;
                loadRows();
            }
        }
    }
}
