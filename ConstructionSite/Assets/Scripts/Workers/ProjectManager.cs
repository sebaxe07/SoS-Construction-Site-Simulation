using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace constructionSite.Scripts
{
    public class ProjectManager : MonoBehaviour
    {

        private int _id;
        private string _name;
        private List<Worker> _workers;

        private List<Machine> _machinesOwned;

        public List<Machine> MachinesOwned
        {
            get { return _machinesOwned; }
            set { _machinesOwned = value; }
        }
        // private List<ConstructionSite> _constructionSites;
        // private List<Machine> _machines;

        // public ProjectManager(int id, string name, List<Worker> workers, List<ConstructionSite> constructionSites, List<Machine> machines)
        // {
        //     _id = id;
        //     _name = name;
        //     _workers = workers;
        //     _constructionSites = constructionSites;
        //     _machines = machines;
        // }

        // Method to initialize fields
        public void Initialize(int id, string name)
        {
            _id = id;
            _name = name;
            _workers = new List<Worker>(); // Initializes a new empty list of workers
            _machinesOwned = new List<Machine>(); // Initializes a new empty list of machines
            Instance = this;
            MachineManager.LoadMachinesStatic();
            HelperWorkers.LoadWorkersFromJson(this);
        }

        public void addWorker(Worker worker)
        {
            _workers.Add(worker);
        }
        public void addWorker()
        {
            Worker worker = new Worker(HelperWorkers.GenerateWorkerUniqueId(_workers), "Name", "Surname",
            Worker.Role.AllRounder, Worker.WorkerStatus.Idle);
            _workers.Add(worker);
        }

        public void addWorkers(int number)
        {
            for (int i = 0; i < number; i++)
            {
                addWorker();
            }
        }
        public void deleteWorker(int id)
        {
            Worker worker = _workers.Find(worker => worker.Id.Equals(id));
            if (worker != null)
            {
                _workers.Remove(worker);
            }
        }

        public int getId()
        {
            return _id;
        }

        public string getName()
        {
            return _name;
        }

        public List<Worker> Workers
        {
            get => _workers;
        }

        public void RemoveMachine(int id)
        {
            Machine machine = _machinesOwned.Find(machine => machine.MachineID.Equals(id));
            if (machine != null)
            {
                _machinesOwned.Remove(machine);
            }
        }

        public static ProjectManager Instance { get; private set; }

        // public List<ConstructionSite> getConstructionSites() 
        // { 
        //     return _constructionSites; 
        // }

        // public List<Machine> getMachines() 
        // { 
        //     return _machines; 
        // }
    }
}
