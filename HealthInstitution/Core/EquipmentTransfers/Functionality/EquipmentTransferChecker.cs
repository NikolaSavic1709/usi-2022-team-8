﻿using HealthInstitution.Core.Equipments;
using HealthInstitution.Core.Equipments.Model;
using HealthInstitution.Core.Equipments.Repository;
using HealthInstitution.Core.EquipmentTransfers.Model;
using HealthInstitution.Core.EquipmentTransfers.Repository;
using HealthInstitution.Core.Rooms;
using HealthInstitution.Core.Rooms.Model;
using HealthInstitution.Core.Rooms.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthInstitution.Core.EquipmentTransfers.Functionality
{
    public class EquipmentTransferChecker
    {
        static EquipmentRepository s_equipmentRepository = EquipmentRepository.GetInstance();
        static RoomRepository s_roomRepository = RoomRepository.GetInstance();
        static EquipmentTransferRepository s_equipmentTransferRepository = EquipmentTransferRepository.GetInstance();
        public static void UpdateByTransfer()
        {
            List<int> equipmentTransfersToRemove = new List<int>();
            
            foreach (EquipmentTransfer equipmentTransfer in EquipmentTransferRepository.GetInstance().GetAll())
            {
                if(equipmentTransfer.ToRoom.Id==1 && equipmentTransfer.TransferTime<=DateTime.Now)
                {
                    FillWarehouse(equipmentTransfer, equipmentTransfersToRemove);
                }
                if (equipmentTransfer.TransferTime <= DateTime.Today)
                {
                    Equipment equipmentFromRoom = equipmentTransfer.FromRoom.AvailableEquipment.Find(eq => (eq.Type == equipmentTransfer.Equipment.Type && eq.Name == equipmentTransfer.Equipment.Name));
                    Transfer(equipmentTransfer.ToRoom, equipmentFromRoom, equipmentTransfer.Equipment.Quantity);
                    equipmentTransfersToRemove.Add(equipmentTransfer.Id);
                }
            }
            RemoveOldTransfers(equipmentTransfersToRemove);
            
        }
        private static void FillWarehouse(EquipmentTransfer equipmentTransfer, List<int> equipmentTransfersToRemove)
        {
            Equipment purchasedEquipment = s_equipmentRepository.EquipmentById[equipmentTransfer.Equipment.Id];
            Transfer(equipmentTransfer.ToRoom, purchasedEquipment, purchasedEquipment.Quantity);
            equipmentTransfersToRemove.Add(equipmentTransfer.Id);
        }

        private static void RemoveOldTransfers(List<int> equipmentTransfersToRemove)
        {
            foreach (int id in equipmentTransfersToRemove)
            {
                EquipmentTransferService.Delete(id);
            }
        }

        public static void Transfer(Room toRoom, Equipment equipment, int quantity)
        {
            equipment.Quantity -= quantity;
            int index = toRoom.AvailableEquipment.FindIndex(eq => (eq.Name == equipment.Name && eq.Type == equipment.Type));
            if (index >= 0)
            {
                toRoom.AvailableEquipment[index].Quantity += quantity;
                s_equipmentRepository.Save();
            }
            else
            {
                EquipmentDTO equipmentDTO = new EquipmentDTO(quantity, equipment.Name, equipment.Type, equipment.IsDynamic);
                Equipment newEquipment =(toRoom.Id==1)?equipment: EquipmentService.Add(equipmentDTO);
                RoomService.AddToRoom(toRoom.Id, newEquipment);
            }
        }

        public static void Transfer(List<Equipment> toRoomEquipments, Equipment equipment, int quantity)
        {
            equipment.Quantity -= quantity;
            int index = toRoomEquipments.FindIndex(eq => (eq.Name == equipment.Name && eq.Type == equipment.Type));
            if (index >= 0)
            {
                toRoomEquipments[index].Quantity += quantity;
                s_equipmentRepository.Save();
            }
            else
            {
                EquipmentDTO equipmentDTO = new EquipmentDTO(quantity, equipment.Name, equipment.Type, equipment.IsDynamic);
                Equipment newEquipment = EquipmentService.Add(equipmentDTO);
                toRoomEquipments.Add(newEquipment);
            }
        }
    }
}
