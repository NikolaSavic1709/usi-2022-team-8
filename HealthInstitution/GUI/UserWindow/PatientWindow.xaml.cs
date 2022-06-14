﻿using HealthInstitution.GUI.PatientWindows;
using System.Windows;

using HealthInstitution.GUI.LoginView;
using HealthInstitution.GUI.PatientView;
using HealthInstitution.Core.SystemUsers.Patients.Model;
using HealthInstitution.Core.Notifications.Model;
using HealthInstitution.Core.SystemUsers.Patients;

namespace HealthInstitution.GUI.UserWindow
{
    /// <summary>
    /// Interaction logic for PatientWindow.xaml
    /// </summary>
    public partial class PatientWindow : Window
    {
        private Patient _loggedPatient;
        IPatientService _patientService;
        public PatientWindow(Patient loggedPatient, IPatientService patientService)
        {
            InitializeComponent();
            this._loggedPatient = loggedPatient;
            this._patientService = patientService;
            ShowNotificationsDialog();
            new RecepieNotificationDialog(loggedPatient.Username).ShowDialog();
        }

        private void ShowNotificationsDialog()
        {
            if (_patientService.GetActiveAppointmentNotification(_loggedPatient).Count>0)
            {
                PatientNotificationsDialog patientNotificationsDialog = new PatientNotificationsDialog(this._loggedPatient);
                patientNotificationsDialog.ShowDialog();
            }
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to log out?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                this.Close();
                LoginWindow window = new LoginWindow();
                window.ShowDialog();
            }
        }

        private void ManuallSchedule_Click(object sender, RoutedEventArgs e)
        {
            new PatientScheduleWindow(this._loggedPatient).ShowDialog();
        }

        private void RecommendedSchedule_Click(object sender, RoutedEventArgs e)
        {
            new RecommendedWindow(this._loggedPatient).ShowDialog();
        }

        private void MedicalRecordView_button_Click(object sender, RoutedEventArgs e)
        {
            new MedicalRecordView(_loggedPatient).ShowDialog();
        }

        private void PickDoctor_button_Click(object sender, RoutedEventArgs e)
        {
            new DoctorPickExamination(_loggedPatient).ShowDialog();
        }

        private void RecepieNotificationSettings_button_Click(object sender, RoutedEventArgs e)
        {
            new RecepieNotificationSettingsDialog(_loggedPatient.Username).ShowDialog();
        }

        private void rateHospital_button_Click(object sender, RoutedEventArgs e)
        {
            new PatientHospitalPollDialog().ShowDialog();
        }
    }
}