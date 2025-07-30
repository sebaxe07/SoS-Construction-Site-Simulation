using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace constructionSite.Scripts
{
    static public class HelperMachines
    {
        public static int GenerateMachineUniqueId()
        {
            List<Machine> existingMachines = ProjectManager.Instance.MachinesOwned;
            if (existingMachines.Count == 0) return 1;
            return existingMachines.Max(m => m.MachineID) + 1;
        }
    }
}