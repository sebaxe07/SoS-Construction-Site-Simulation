using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace constructionSite.Scripts
{
    public class Worker
    {
        private int _id;

        private string _name;

        private string _surname;

        private Role _role;
        /*Roles are defined here*/
        private WorkerStatus _status;
        public enum Role
        {
            AllRounder = 1,           // All rounder - average operation speed
            HeavyMachineOperator = 2, // Specialized in heavy machinery (excavators, cranes) - faster operation
            LightMachineOperator = 3, // Specialized in light machinery (small tools, lifts) - faster operation
            TruckDriver = 4,          // Specialized in driving trucks - faster operation
            Trainee = 5,              // Trainee - slower operation
        }

        public enum WorkerStatus
        {
            Idle = 1,
            Busy = 2
        }

        public WorkerStatus Status
        {
            get => _status;
            set => _status = value;
        }

        public Worker(int id, string name, string surname, Role role, WorkerStatus status)
        {
            _id = id;
            _name = name;
            _surname = surname;
            _role = role;
            _status = status;
        }

        public int Id
        {
            get => _id;
            set => _id = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Surname
        {
            get => _surname;
            set => _surname = value;
        }

        public string GetRole()
        {
            return System.Enum.GetName(typeof(Role), _role);
        }

        public void SetRole(Role role)
        {
            _role = role;
        }

    }
}
