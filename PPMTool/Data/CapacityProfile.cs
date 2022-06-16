using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    public class CapacityProfile
    {
        // Basically this needs to be an object containin a person and then a list of objects representing every subtask on the system with a flag that says whether that person is assigned to it or not.
        // When we plot the apex chart, we give it each object and we use a flag to dictate whether each object has a value for each series since each series will represent a subtask.



        //public class Assignment
        //{
        //    public string ProjectName { get; }

        //    public SubTask SubTask { get; }

        //    public Assignment(string projectName, SubTask subTask)
        //    {
        //        ProjectName = projectName;
        //        SubTask = subTask;
        //    }
        //}

        //public Person Person { get; }

        ///// <summary>
        ///// TODO: This needs to be a list of every project
        ///// </summary>
        //public IEnumerable<Assignment> Assignments { get; }

        //public CapacityProfile(Person person, IEnumerable<Assignment> assignments)
        //{
        //    Person = person;
        //    Assignments = assignments;
        //}
    }
}
