using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NeoServiceLayer.Services
{
    /// <summary>
    /// Advanced healthcare management service for medical records and patient care
    /// Supports secure medical data, telemedicine, and healthcare analytics
    /// </summary>
    [DisplayName("HealthcareContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Advanced healthcare management service")]
    [ManifestExtra("Version", "1.0.0")]
    [ContractPermission("*", "*")]
    public class HealthcareContract : SmartContract, IServiceContract
    {
        #region Constants
        private const string SERVICE_NAME = "Healthcare";
        private const byte HEALTHCARE_PREFIX = 0x48; // 'H'
        private const byte PATIENTS_PREFIX = 0x50;
        private const byte PROVIDERS_PREFIX = 0x50;
        private const byte RECORDS_PREFIX = 0x52;
        private const byte APPOINTMENTS_PREFIX = 0x41;
        private const byte PRESCRIPTIONS_PREFIX = 0x50;
        #endregion

        #region Events
        [DisplayName("PatientRegistered")]
        public static event Action<string, UInt160, BigInteger> OnPatientRegistered;

        [DisplayName("ProviderRegistered")]
        public static event Action<string, UInt160, byte, string> OnProviderRegistered;

        [DisplayName("MedicalRecordCreated")]
        public static event Action<string, string, UInt160, BigInteger> OnMedicalRecordCreated;

        [DisplayName("AppointmentScheduled")]
        public static event Action<string, string, string, BigInteger> OnAppointmentScheduled;

        [DisplayName("PrescriptionIssued")]
        public static event Action<string, string, UInt160, string> OnPrescriptionIssued;

        [DisplayName("HealthcareError")]
        public static event Action<string, string> OnHealthcareError;
        #endregion

        #region Data Structures
        public enum ProviderType : byte
        {
            Doctor = 0,
            Nurse = 1,
            Specialist = 2,
            Hospital = 3,
            Clinic = 4,
            Laboratory = 5,
            Pharmacy = 6,
            Therapist = 7
        }

        public enum RecordType : byte
        {
            Diagnosis = 0,
            Treatment = 1,
            Prescription = 2,
            LabResult = 3,
            Imaging = 4,
            Surgery = 5,
            Vaccination = 6,
            Allergy = 7
        }

        public enum AppointmentStatus : byte
        {
            Scheduled = 0,
            Confirmed = 1,
            InProgress = 2,
            Completed = 3,
            Cancelled = 4,
            NoShow = 5,
            Rescheduled = 6
        }

        public enum PrescriptionStatus : byte
        {
            Prescribed = 0,
            Dispensed = 1,
            Completed = 2,
            Cancelled = 3,
            Expired = 4
        }

        public class Patient
        {
            public string Id;
            public UInt160 Owner;
            public string Name;
            public BigInteger DateOfBirth;
            public byte Gender;
            public string BloodType;
            public string[] Allergies;
            public string[] ChronicConditions;
            public string EmergencyContact;
            public BigInteger RegisteredAt;
            public string[] AuthorizedProviders;
            public bool IsActive;
            public string EncryptedData;
        }

        public class HealthcareProvider
        {
            public string Id;
            public UInt160 Owner;
            public string Name;
            public ProviderType Type;
            public string LicenseNumber;
            public string Specialization;
            public string Location;
            public BigInteger RegisteredAt;
            public string[] Certifications;
            public BigInteger Rating;
            public bool IsVerified;
            public bool IsActive;
        }

        public class MedicalRecord
        {
            public string Id;
            public string PatientId;
            public UInt160 Provider;
            public RecordType Type;
            public BigInteger CreatedAt;
            public string Diagnosis;
            public string Treatment;
            public string Notes;
            public string[] Attachments;
            public bool IsConfidential;
            public string EncryptedData;
            public BigInteger LastUpdated;
        }

        public class Appointment
        {
            public string Id;
            public string PatientId;
            public string ProviderId;
            public BigInteger ScheduledTime;
            public BigInteger Duration;
            public AppointmentStatus Status;
            public string Purpose;
            public string Location;
            public BigInteger CreatedAt;
            public string Notes;
            public bool IsTelemedicine;
            public string MeetingLink;
        }

        public class Prescription
        {
            public string Id;
            public string PatientId;
            public UInt160 Prescriber;
            public string Medication;
            public string Dosage;
            public string Instructions;
            public BigInteger IssuedAt;
            public BigInteger ExpiresAt;
            public PrescriptionStatus Status;
            public BigInteger RefillsRemaining;
            public string PharmacyId;
            public bool IsControlled;
        }

        public class LabResult
        {
            public string Id;
            public string PatientId;
            public UInt160 Laboratory;
            public string TestType;
            public BigInteger TestDate;
            public string Results;
            public string ReferenceRanges;
            public bool IsAbnormal;
            public string TechnicianNotes;
            public bool IsVerified;
        }
        #endregion

        #region Storage Keys
        private static StorageKey PatientKey(string id) => new byte[] { PATIENTS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey ProviderKey(string id) => new byte[] { PROVIDERS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey RecordKey(string id) => new byte[] { RECORDS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey AppointmentKey(string id) => new byte[] { APPOINTMENTS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        private static StorageKey PrescriptionKey(string id) => new byte[] { PRESCRIPTIONS_PREFIX }.Concat(Utility.StrictUTF8Encode(id));
        #endregion

        #region IServiceContract Implementation
        public static string GetServiceName() => SERVICE_NAME;

        public static string GetServiceVersion() => "1.0.0";

        public static string[] GetServiceMethods() => new string[]
        {
            "RegisterPatient",
            "RegisterProvider",
            "CreateMedicalRecord",
            "ScheduleAppointment",
            "IssuePrescription",
            "GetPatientRecords",
            "AuthorizeProvider",
            "UpdateAppointmentStatus",
            "GetProviderInfo"
        };

        public static bool IsServiceActive() => true;

        protected static void ValidateAccess()
        {
            if (!Runtime.CheckWitness(Runtime.CallingScriptHash))
                throw new InvalidOperationException("Unauthorized access");
        }

        public static object ExecuteServiceOperation(string method, object[] args)
        {
            return ExecuteServiceOperation<object>(method, args);
        }

        protected static T ExecuteServiceOperation<T>(string method, object[] args)
        {
            ValidateAccess();
            
            switch (method)
            {
                case "RegisterPatient":
                    return (T)(object)RegisterPatient((string)args[0], (BigInteger)args[1], (byte)args[2], (string)args[3], (string[])args[4], (string[])args[5], (string)args[6]);
                case "RegisterProvider":
                    return (T)(object)RegisterProvider((string)args[0], (byte)args[1], (string)args[2], (string)args[3], (string)args[4], (string[])args[5]);
                case "CreateMedicalRecord":
                    return (T)(object)CreateMedicalRecord((string)args[0], (byte)args[1], (string)args[2], (string)args[3], (string)args[4], (bool)args[5]);
                case "ScheduleAppointment":
                    return (T)(object)ScheduleAppointment((string)args[0], (string)args[1], (BigInteger)args[2], (BigInteger)args[3], (string)args[4], (string)args[5], (bool)args[6]);
                case "IssuePrescription":
                    return (T)(object)IssuePrescription((string)args[0], (string)args[1], (string)args[2], (string)args[3], (BigInteger)args[4], (BigInteger)args[5], (bool)args[6]);
                case "GetPatientRecords":
                    return (T)(object)GetPatientRecords((string)args[0]);
                case "AuthorizeProvider":
                    return (T)(object)AuthorizeProvider((string)args[0], (string)args[1]);
                case "UpdateAppointmentStatus":
                    return (T)(object)UpdateAppointmentStatus((string)args[0], (byte)args[1], (string)args[2]);
                case "GetProviderInfo":
                    return (T)(object)GetProviderInfo((string)args[0]);
                default:
                    throw new InvalidOperationException($"Unknown method: {method}");
            }
        }
        #endregion

        #region Patient Management
        /// <summary>
        /// Register a new patient
        /// </summary>
        public static string RegisterPatient(string name, BigInteger dateOfBirth, byte gender, string bloodType, string[] allergies, string[] chronicConditions, string emergencyContact)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Patient name required");
            if (dateOfBirth <= 0) throw new ArgumentException("Valid date of birth required");

            try
            {
                var patientId = GenerateId("PAT");
                var patient = new Patient
                {
                    Id = patientId,
                    Owner = Runtime.CallingScriptHash,
                    Name = name,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    BloodType = bloodType ?? "",
                    Allergies = allergies ?? new string[0],
                    ChronicConditions = chronicConditions ?? new string[0],
                    EmergencyContact = emergencyContact ?? "",
                    RegisteredAt = Runtime.Time,
                    AuthorizedProviders = new string[0],
                    IsActive = true,
                    EncryptedData = ""
                };

                Storage.Put(Storage.CurrentContext, PatientKey(patientId), StdLib.Serialize(patient));
                OnPatientRegistered(patientId, Runtime.CallingScriptHash, Runtime.Time);

                return patientId;
            }
            catch (Exception ex)
            {
                OnHealthcareError("RegisterPatient", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Authorize a healthcare provider to access patient records
        /// </summary>
        public static bool AuthorizeProvider(string patientId, string providerId)
        {
            if (string.IsNullOrEmpty(patientId)) throw new ArgumentException("Patient ID required");
            if (string.IsNullOrEmpty(providerId)) throw new ArgumentException("Provider ID required");

            var patientData = Storage.Get(Storage.CurrentContext, PatientKey(patientId));
            if (patientData == null) throw new InvalidOperationException("Patient not found");

            var patient = (Patient)StdLib.Deserialize(patientData);
            if (patient.Owner != Runtime.CallingScriptHash) throw new UnauthorizedAccessException("Not patient owner");

            try
            {
                // Add provider to authorized list (simplified implementation)
                patient.AuthorizedProviders = AddToArray(patient.AuthorizedProviders, providerId);
                Storage.Put(Storage.CurrentContext, PatientKey(patientId), StdLib.Serialize(patient));

                return true;
            }
            catch (Exception ex)
            {
                OnHealthcareError("AuthorizeProvider", ex.Message);
                return false;
            }
        }
        #endregion

        #region Provider Management
        /// <summary>
        /// Register a new healthcare provider
        /// </summary>
        public static string RegisterProvider(string name, byte providerType, string licenseNumber, string specialization, string location, string[] certifications)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Provider name required");
            if (!Enum.IsDefined(typeof(ProviderType), providerType)) throw new ArgumentException("Invalid provider type");
            if (string.IsNullOrEmpty(licenseNumber)) throw new ArgumentException("License number required");

            try
            {
                var providerId = GenerateId("PRV");
                var provider = new HealthcareProvider
                {
                    Id = providerId,
                    Owner = Runtime.CallingScriptHash,
                    Name = name,
                    Type = (ProviderType)providerType,
                    LicenseNumber = licenseNumber,
                    Specialization = specialization ?? "",
                    Location = location ?? "",
                    RegisteredAt = Runtime.Time,
                    Certifications = certifications ?? new string[0],
                    Rating = 0,
                    IsVerified = false, // Requires verification process
                    IsActive = true
                };

                Storage.Put(Storage.CurrentContext, ProviderKey(providerId), StdLib.Serialize(provider));
                OnProviderRegistered(providerId, Runtime.CallingScriptHash, providerType, specialization);

                return providerId;
            }
            catch (Exception ex)
            {
                OnHealthcareError("RegisterProvider", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get provider information
        /// </summary>
        public static HealthcareProvider GetProviderInfo(string providerId)
        {
            if (string.IsNullOrEmpty(providerId)) throw new ArgumentException("Provider ID required");

            var data = Storage.Get(Storage.CurrentContext, ProviderKey(providerId));
            if (data == null) return null;

            return (HealthcareProvider)StdLib.Deserialize(data);
        }
        #endregion

        #region Medical Records
        /// <summary>
        /// Create a new medical record
        /// </summary>
        public static string CreateMedicalRecord(string patientId, byte recordType, string diagnosis, string treatment, string notes, bool isConfidential)
        {
            if (string.IsNullOrEmpty(patientId)) throw new ArgumentException("Patient ID required");
            if (!Enum.IsDefined(typeof(RecordType), recordType)) throw new ArgumentException("Invalid record type");

            var patientData = Storage.Get(Storage.CurrentContext, PatientKey(patientId));
            if (patientData == null) throw new InvalidOperationException("Patient not found");

            var patient = (Patient)StdLib.Deserialize(patientData);
            
            // Check if provider is authorized
            if (!IsProviderAuthorized(patient.AuthorizedProviders, Runtime.CallingScriptHash.ToString()))
                throw new UnauthorizedAccessException("Provider not authorized");

            try
            {
                var recordId = GenerateId("REC");
                var record = new MedicalRecord
                {
                    Id = recordId,
                    PatientId = patientId,
                    Provider = Runtime.CallingScriptHash,
                    Type = (RecordType)recordType,
                    CreatedAt = Runtime.Time,
                    Diagnosis = diagnosis ?? "",
                    Treatment = treatment ?? "",
                    Notes = notes ?? "",
                    Attachments = new string[0],
                    IsConfidential = isConfidential,
                    EncryptedData = "",
                    LastUpdated = Runtime.Time
                };

                Storage.Put(Storage.CurrentContext, RecordKey(recordId), StdLib.Serialize(record));
                OnMedicalRecordCreated(recordId, patientId, Runtime.CallingScriptHash, Runtime.Time);

                return recordId;
            }
            catch (Exception ex)
            {
                OnHealthcareError("CreateMedicalRecord", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get patient medical records
        /// </summary>
        public static string[] GetPatientRecords(string patientId)
        {
            if (string.IsNullOrEmpty(patientId)) throw new ArgumentException("Patient ID required");

            var patientData = Storage.Get(Storage.CurrentContext, PatientKey(patientId));
            if (patientData == null) throw new InvalidOperationException("Patient not found");

            var patient = (Patient)StdLib.Deserialize(patientData);
            
            // Check access permissions
            if (patient.Owner != Runtime.CallingScriptHash && 
                !IsProviderAuthorized(patient.AuthorizedProviders, Runtime.CallingScriptHash.ToString()))
                throw new UnauthorizedAccessException("Access denied");

            // Simplified implementation - would return actual record IDs
            var records = new string[0];
            return records;
        }
        #endregion

        #region Appointments
        /// <summary>
        /// Schedule a new appointment
        /// </summary>
        public static string ScheduleAppointment(string patientId, string providerId, BigInteger scheduledTime, BigInteger duration, string purpose, string location, bool isTelemedicine)
        {
            if (string.IsNullOrEmpty(patientId)) throw new ArgumentException("Patient ID required");
            if (string.IsNullOrEmpty(providerId)) throw new ArgumentException("Provider ID required");
            if (scheduledTime <= Runtime.Time) throw new ArgumentException("Scheduled time must be in the future");

            // Verify patient and provider exist
            var patientData = Storage.Get(Storage.CurrentContext, PatientKey(patientId));
            var providerData = Storage.Get(Storage.CurrentContext, ProviderKey(providerId));
            if (patientData == null) throw new InvalidOperationException("Patient not found");
            if (providerData == null) throw new InvalidOperationException("Provider not found");

            try
            {
                var appointmentId = GenerateId("APT");
                var appointment = new Appointment
                {
                    Id = appointmentId,
                    PatientId = patientId,
                    ProviderId = providerId,
                    ScheduledTime = scheduledTime,
                    Duration = duration,
                    Status = AppointmentStatus.Scheduled,
                    Purpose = purpose ?? "",
                    Location = location ?? "",
                    CreatedAt = Runtime.Time,
                    Notes = "",
                    IsTelemedicine = isTelemedicine,
                    MeetingLink = isTelemedicine ? GenerateMeetingLink() : ""
                };

                Storage.Put(Storage.CurrentContext, AppointmentKey(appointmentId), StdLib.Serialize(appointment));
                OnAppointmentScheduled(appointmentId, patientId, providerId, scheduledTime);

                return appointmentId;
            }
            catch (Exception ex)
            {
                OnHealthcareError("ScheduleAppointment", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Update appointment status
        /// </summary>
        public static bool UpdateAppointmentStatus(string appointmentId, byte status, string notes)
        {
            if (string.IsNullOrEmpty(appointmentId)) throw new ArgumentException("Appointment ID required");
            if (!Enum.IsDefined(typeof(AppointmentStatus), status)) throw new ArgumentException("Invalid status");

            var appointmentData = Storage.Get(Storage.CurrentContext, AppointmentKey(appointmentId));
            if (appointmentData == null) throw new InvalidOperationException("Appointment not found");

            try
            {
                var appointment = (Appointment)StdLib.Deserialize(appointmentData);
                appointment.Status = (AppointmentStatus)status;
                appointment.Notes = notes ?? appointment.Notes;

                Storage.Put(Storage.CurrentContext, AppointmentKey(appointmentId), StdLib.Serialize(appointment));
                return true;
            }
            catch (Exception ex)
            {
                OnHealthcareError("UpdateAppointmentStatus", ex.Message);
                return false;
            }
        }
        #endregion

        #region Prescriptions
        /// <summary>
        /// Issue a new prescription
        /// </summary>
        public static string IssuePrescription(string patientId, string medication, string dosage, string instructions, BigInteger expiresAt, BigInteger refills, bool isControlled)
        {
            if (string.IsNullOrEmpty(patientId)) throw new ArgumentException("Patient ID required");
            if (string.IsNullOrEmpty(medication)) throw new ArgumentException("Medication required");
            if (string.IsNullOrEmpty(dosage)) throw new ArgumentException("Dosage required");

            var patientData = Storage.Get(Storage.CurrentContext, PatientKey(patientId));
            if (patientData == null) throw new InvalidOperationException("Patient not found");

            try
            {
                var prescriptionId = GenerateId("PRX");
                var prescription = new Prescription
                {
                    Id = prescriptionId,
                    PatientId = patientId,
                    Prescriber = Runtime.CallingScriptHash,
                    Medication = medication,
                    Dosage = dosage,
                    Instructions = instructions ?? "",
                    IssuedAt = Runtime.Time,
                    ExpiresAt = expiresAt,
                    Status = PrescriptionStatus.Prescribed,
                    RefillsRemaining = refills,
                    PharmacyId = "",
                    IsControlled = isControlled
                };

                Storage.Put(Storage.CurrentContext, PrescriptionKey(prescriptionId), StdLib.Serialize(prescription));
                OnPrescriptionIssued(prescriptionId, patientId, Runtime.CallingScriptHash, medication);

                return prescriptionId;
            }
            catch (Exception ex)
            {
                OnHealthcareError("IssuePrescription", ex.Message);
                throw;
            }
        }
        #endregion

        #region Utility Methods
        private static string GenerateId(string prefix)
        {
            var timestamp = Runtime.Time;
            var random = Runtime.GetRandom();
            return $"{prefix}_{timestamp}_{random}";
        }

        private static string GenerateMeetingLink()
        {
            var random = Runtime.GetRandom();
            return $"https://telemedicine.neo/{random}";
        }

        private static bool IsProviderAuthorized(string[] authorizedProviders, string providerId)
        {
            if (authorizedProviders == null) return false;
            
            foreach (var authorized in authorizedProviders)
            {
                if (authorized == providerId) return true;
            }
            return false;
        }

        private static string[] AddToArray(string[] array, string item)
        {
            var newArray = new string[array.Length + 1];
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }
            newArray[array.Length] = item;
            return newArray;
        }
        #endregion

        #region Administrative Methods
        /// <summary>
        /// Get healthcare service statistics
        /// </summary>
        public static Map<string, BigInteger> GetHealthcareStats()
        {
            var stats = new Map<string, BigInteger>();
            stats["total_patients"] = GetTotalPatients();
            stats["total_providers"] = GetTotalProviders();
            stats["total_records"] = GetTotalRecords();
            stats["total_appointments"] = GetTotalAppointments();
            stats["total_prescriptions"] = GetTotalPrescriptions();
            return stats;
        }

        private static BigInteger GetTotalPatients()
        {
            return Storage.Get(Storage.CurrentContext, "total_patients")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalProviders()
        {
            return Storage.Get(Storage.CurrentContext, "total_providers")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalRecords()
        {
            return Storage.Get(Storage.CurrentContext, "total_records")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalAppointments()
        {
            return Storage.Get(Storage.CurrentContext, "total_appointments")?.ToBigInteger() ?? 0;
        }

        private static BigInteger GetTotalPrescriptions()
        {
            return Storage.Get(Storage.CurrentContext, "total_prescriptions")?.ToBigInteger() ?? 0;
        }
        #endregion
    }
}