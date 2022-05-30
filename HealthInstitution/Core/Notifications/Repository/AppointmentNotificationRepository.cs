﻿using HealthInstitution.Core.Examinations.Model;
using HealthInstitution.Core.Examinations.Repository;
using HealthInstitution.Core.Notifications.Model;
using HealthInstitution.Core.Operations.Repository;
using HealthInstitution.Core.SystemUsers.Doctors.Model;
using HealthInstitution.Core.SystemUsers.Doctors.Repository;
using HealthInstitution.Core.SystemUsers.Patients.Model;
using HealthInstitution.Core.SystemUsers.Patients.Repository;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HealthInstitution.Core.Notifications.Repository
{
    internal class AppointmentNotificationRepository
    {
        private String _fileName;
        public int _maxId { get; set; }
        public List<AppointmentNotification> Notifications { get; set; }
        public Dictionary<int, AppointmentNotification> NotificationsById { get; set; }

        private JsonSerializerOptions _options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };
        private AppointmentNotificationRepository(String fileName)
        {
            this._fileName = fileName;
            this.Notifications = new List<AppointmentNotification>();
            this.NotificationsById = new Dictionary<int, AppointmentNotification>();
            this._maxId = 0;
            this.LoadFromFile();
        }
        private static AppointmentNotificationRepository s_instance = null;
        public static AppointmentNotificationRepository GetInstance()
        {
            {
                if (s_instance == null)
                {
                    s_instance = new AppointmentNotificationRepository(@"..\..\..\Data\JSON\notifications.json");
                }
                return s_instance;
            }
        }
        public void LoadFromFile()
        { 
            var allNotifications = JArray.Parse(File.ReadAllText(this._fileName));
            foreach (var notification in allNotifications)
            {
                int id = (int)notification["id"];
                DateTime? oldAppointment = (DateTime?)notification["oldAppointment"];
                DateTime newAppointment = (DateTime)notification["newAppointment"];
                bool activeForDoctor = (bool)notification["activeForDoctor"];
                bool activeForPatient = (bool)notification["activeForPatient"];

                AppointmentNotification loadedNotification = new AppointmentNotification(id,oldAppointment,newAppointment,null,null,activeForDoctor, activeForPatient);

                if (id > _maxId) { _maxId = id; }

                this.Notifications.Add(loadedNotification);
                this.NotificationsById.Add(id, loadedNotification);
            }
        }


        public void Save()
        {
            List<dynamic> reducedNotifications = new List<dynamic>();
            foreach (AppointmentNotification notification in this.Notifications)
            {
                reducedNotifications.Add(new
                {
                    id = notification.Id,
                    oldAppointment = notification.OldAppointment,
                    newAppointment = notification.NewAppointment,
                    activeForDoctor = notification.ActiveForDoctor,
                    activeForPatient=notification.ActiveForPatient
                }) ;
            }
            var allNotifications = JsonSerializer.Serialize(reducedNotifications, _options);
            File.WriteAllText(this._fileName, allNotifications);
        }

        public List<AppointmentNotification> GetAll()
        {
            return this.Notifications;
        }

        public AppointmentNotification GetById(int id)
        {
            if (NotificationsById.ContainsKey(id))
            {
                return NotificationsById[id];
            }
            return null;
        }

        public void Add(AppointmentNotificationDTO appointmentNotificationDTO)
        {
            int id = ++this._maxId;
            AppointmentNotification notification;
            if (appointmentNotificationDTO.OldAppointment==null)
                notification = new AppointmentNotification(id, null, appointmentNotificationDTO.NewAppointment, appointmentNotificationDTO.Doctor, appointmentNotificationDTO.Patient, true,true);
            else
                notification = new AppointmentNotification(id, appointmentNotificationDTO.OldAppointment, appointmentNotificationDTO.NewAppointment, appointmentNotificationDTO.Doctor, appointmentNotificationDTO.Patient, true, true);
            appointmentNotificationDTO.Doctor.Notifications.Add(notification);
            appointmentNotificationDTO.Patient.Notifications.Add(notification);
            this.Notifications.Add(notification);
            this.NotificationsById.Add(id, notification);
            Save();
            AppointmentNotificationPatientRepository.GetInstance().Save();
            AppointmentNotificationDoctorRepository.GetInstance().Save();

        }

        public void Delete(int id)
        {
            AppointmentNotification notification=NotificationsById[id];
            this.Notifications.Remove(notification);
            this.NotificationsById.Remove(id);
            Save();
        }
    }
}