using DukeBox.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.model; 

public interface ICategory {
    string Name { get; }
    int SortOrder { get; }
    uint Color { get; }
    string IconPath { get; }
}

public interface ICategoryStore {
    IEnumerable<ICategory> List();
    ICategory? Get(long id);
    ICategory? GetByName(string name);
}

public interface IMutableCategoryStore : ICategoryStore {
    ICategory Add(string name, int sortOrder = 0, uint color = 0, string iconPath = "");
    //void Remove(ICategory entry);
    //void Remove(long id);
    //void Remove(string name);
}

