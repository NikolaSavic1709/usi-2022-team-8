﻿using HealthInstitution.Core.Examinations.Model;
using HealthInstitution.Core.MedicalRecords.Model;
using HealthInstitution.Core.MedicalRecords.Repository;
using HealthInstitution.Core.Rooms.Model;
using HealthInstitution.Core.Rooms.Repository;
using HealthInstitution.Core.SystemUsers.Doctors.Model;
using HealthInstitution.Core.SystemUsers.Doctors.Repository;
using HealthInstitution.Core.SystemUsers.Patients.Repository;
using HealthInstitution.Core.SystemUsers.Patients.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HealthInstitution.Core.Operations.Repository;
using HealthInstitution.Core.Operations.Model;

namespace HealthInstitution.Core.Examinations.Repository;

internal class ExaminationRepository
{
    private String _fileName;
    public int _maxId { get; set; }
    public List<Examination> Examinations { get; set; }
    public Dictionary<int, Examination> ExaminationsById { get; set; }

    private JsonSerializerOptions _options = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private ExaminationRepository(String fileName)
    {
        this._fileName = fileName;
        this.Examinations = new List<Examination>();
        this.ExaminationsById = new Dictionary<int, Examination>();
        this._maxId = 0;
        this.LoadFromFile();
    }

    private static ExaminationRepository s_instance = null;

    public static ExaminationRepository GetInstance()
    {
        {
            if (s_instance == null)
            {
                s_instance = new ExaminationRepository(@"..\..\..\Data\JSON\examinations.json");
            }
            return s_instance;
        }
    }

    private Examination Parse(JToken? examination)
    {
        Dictionary<int, Room> roomsById = RoomRepository.GetInstance().RoomById;
        Dictionary<String, MedicalRecord> medicalRecordsByUsername = MedicalRecordRepository.GetInstance().MedicalRecordByUsername;

        int id = (int)examination["id"];
        ExaminationStatus status;
        Enum.TryParse(examination["status"].ToString(), out status);
        DateTime appointment = (DateTime)examination["appointment"];
        int roomId = (int)examination["room"];
        Room room = roomsById[roomId];
        String doctorUsername = (String)examination["doctor"];
        String patientUsername = (String)examination["medicalRecord"];
        MedicalRecord medicalRecord = medicalRecordsByUsername[patientUsername];
        String anamnesis = (String)examination["anamnesis"];

        return new Examination(id, status, appointment, room, null, medicalRecord, anamnesis);
    }

    public void LoadFromFile()
    {
        var allExaminations = JArray.Parse(File.ReadAllText(this._fileName));
        foreach (var examination in allExaminations)
        {
            Examination loadedExamination = Parse(examination);
            int id = loadedExamination.Id;
            if (id > _maxId) { _maxId = id; }

            this.Examinations.Add(loadedExamination);
            this.ExaminationsById.Add(id, loadedExamination);
        }
    }

    private List<dynamic> PrepareForSerialization()
    {
        List<dynamic> reducedExaminations = new List<dynamic>();
        foreach (Examination examination in this.Examinations)
        {
            reducedExaminations.Add(new
            {
                id = examination.Id,
                status = examination.Status,
                appointment = examination.Appointment,
                room = examination.Room.Id,
                medicalRecord = examination.MedicalRecord.Patient.Username,
                anamnesis = examination.Anamnesis
            });
        }
        return reducedExaminations;
    }

    public void Save()
    {
        List<dynamic> reducedExaminations = PrepareForSerialization();
        var allExaminations = JsonSerializer.Serialize(reducedExaminations, _options);
        File.WriteAllText(this._fileName, allExaminations);
    }

    public List<Examination> GetAll()
    {
        return this.Examinations;
    }

    public Examination GetById(int id)
    {
        if (ExaminationsById.ContainsKey(id))
        {
            return ExaminationsById[id];
        }
        return null;
    }

    public void Add(ExaminationDTO examinationDTO)
    {
        int id = ++this._maxId;
        DateTime appointment = examinationDTO.Appointment;
        Room room = examinationDTO.Room;
        Doctor doctor = examinationDTO.Doctor;
        MedicalRecord medicalRecord = examinationDTO.MedicalRecord;

        Examination examination = new Examination(id, ExaminationStatus.Scheduled, appointment, room, doctor, medicalRecord, "");
        doctor.Examinations.Add(examination);
        this.Examinations.Add(examination);
        this.ExaminationsById.Add(id, examination);

        Save();
        ExaminationDoctorRepository.GetInstance().Save();
    }

    public void Update(int id, ExaminationDTO examinationDTO)
    {
        Examination examination = this.ExaminationsById[id];
        Doctor doctor = examination.Doctor;
        DateTime appointment = examinationDTO.Appointment;
        MedicalRecord medicalRecord = examinationDTO.MedicalRecord;

        CheckIfDoctorIsAvailable(doctor, appointment);
        CheckIfPatientIsAvailable(medicalRecord.Patient, appointment);
        examination.Appointment = appointment;
        examination.MedicalRecord = medicalRecord;
        this.ExaminationsById[id] = examination;

        Save();
    }

    public void Delete(int id)
    {
        Examination examination = this.ExaminationsById[id];
        this.ExaminationsById.Remove(examination.Id);
        this.Examinations.Remove(examination);
        this.ExaminationsById.Remove(id);
        Save();
    }

    private void CheckIfDoctorHasExaminations(Doctor doctor, DateTime dateTime)
    {
        foreach (var examination in doctor.Examinations)
        {
            if (examination.Appointment == dateTime)
            {
                throw new Exception("That doctor is not available");
            }
        }
    }

    private void CheckIfDoctorHasOperations(Doctor doctor, DateTime dateTime) {
        foreach (var operation in doctor.Operations)
        {
            if ((dateTime < operation.Appointment.AddMinutes(operation.Duration)) && (dateTime.AddMinutes(15) > operation.Appointment))
            {
                throw new Exception("That doctor is not available");
            }
        }
    }
    public void CheckIfDoctorIsAvailable(Doctor doctor, DateTime dateTime)
    {
        CheckIfDoctorHasExaminations(doctor, dateTime);
        CheckIfDoctorHasOperations(doctor, dateTime);
    }

    private void CheckIfPatientHasExaminations(Patient patient, DateTime dateTime)
    {
        var allExaminations = this.Examinations;
        foreach (var examination in allExaminations)
        {
            if ((examination.MedicalRecord.Patient.Username == patient.Username) && examination.Appointment == dateTime)
            {
                 throw new Exception("That patient is not available");
            }
        }
    }

    private void CheckIfPatientHasOperations(Patient patient, DateTime dateTime)
    {
        var allOperations = OperationRepository.GetInstance().Operations;
        foreach (var operation in allOperations)
        {
            if (operation.MedicalRecord.Patient.Username == patient.Username)
            {
                if ((dateTime < operation.Appointment.AddMinutes(operation.Duration)) && (dateTime.AddMinutes(15) > operation.Appointment))
                {
                    throw new Exception("That patient is not available");
                }
            }
        }
    }

    private void CheckIfPatientIsAvailable(Patient patient, DateTime dateTime)
    {
        CheckIfPatientHasExaminations(patient, dateTime);
        CheckIfPatientHasOperations(patient, dateTime);    
    }

    private Room FindAvailableRoom(DateTime dateTime)
    {
        bool isAvailable;
        List<Room> availableRooms = new List<Room>();
        var rooms = RoomRepository.GetInstance().GetNotRenovating();
        foreach (var room in rooms)
        {
            if (room.Type != RoomType.ExaminationRoom) continue;
            isAvailable = true;
            foreach (var examination in this.Examinations)
            {
                if (examination.Appointment == dateTime && examination.Room.Id == room.Id)
                {
                    isAvailable = false;
                    break;
                }
            }
            if (isAvailable)
                availableRooms.Add(room);
        }

        if (availableRooms.Count == 0) throw new Exception("There are no available rooms!");

        Random random = new Random();
        int index = random.Next(0, availableRooms.Count);
        return availableRooms[index];
    }

    public void SwapExaminationValue(Examination examination)
    {
        var oldExamination = this.ExaminationsById[examination.Id];
        this.Examinations.Remove(oldExamination);
        this.Examinations.Add(examination);
        examination.Doctor.Examinations.Add(examination);
        oldExamination.Doctor.Examinations.Remove(oldExamination);
        this.ExaminationsById[examination.Id] = examination;
        Save();
    }

    public Examination GenerateRequestExamination(Examination examination, string patientUsername, string doctorUsername, DateTime dateTime)
    {
        Doctor doctor = DoctorRepository.GetInstance().GetById(doctorUsername);
        Patient patient = PatientRepository.GetInstance().GetByUsername(patientUsername);
        CheckIfDoctorIsAvailable(doctor, dateTime);
        CheckIfPatientIsAvailable(patient, dateTime);
        var room = FindAvailableRoom(dateTime);
        Examination e = new Examination(examination.Id, examination.Status, dateTime, room, doctor, examination.MedicalRecord, "");
        return e;
    }

    public void EditExamination(Examination examination, string patientUsername, string doctorUsername, DateTime dateTime)
    {
        Doctor doctor = DoctorRepository.GetInstance().GetById(doctorUsername);
        Patient patient = PatientRepository.GetInstance().GetByUsername(patientUsername);
        CheckIfDoctorIsAvailable(doctor, dateTime);
        CheckIfPatientIsAvailable(patient, dateTime);
        var room = FindAvailableRoom(dateTime);       
        Examination e = new Examination(examination.Id, examination.Status, dateTime, room, doctor, examination.MedicalRecord, "");
        SwapExaminationValue(e);
    }

    public void ReserveExamination(ExaminationDTO examinationDTO)
    {
        Doctor doctor = examinationDTO.Doctor;
        MedicalRecord medicalRecord = examinationDTO.MedicalRecord;
        Patient patient = medicalRecord.Patient;
        DateTime appointment = examinationDTO.Appointment;

       /* CheckIfDoctorIsAvailable(doctor, examinationDTO.Appointment);
        CheckIfPatientIsAvailable(patient, appointment);*/
        var room = FindAvailableRoom(appointment);

        //ExaminationDTO newExamination = new ExaminationDTO(appointment, room, doctor, medicalRecord);
        //Add(newExamination);
        examinationDTO.Room = room;
        Add(examinationDTO);
    }
}