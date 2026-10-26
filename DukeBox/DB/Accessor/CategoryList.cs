using DukeBox.model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DukeBox.DB.Accessor;

public class CategoryList : IMutableCategoryStore {
    private DBConnector _connector;
    private DbSet<CategoryEntry> _categories;
    public CategoryList(DBConnector connector) {
        _connector = connector;
        _categories = connector.Categories;
    }

    public IEnumerable<ICategory> List() {
        return _categories.OrderBy(it => it.SortOrder);
    }

    public ICategory? Get(long id) {
        lock (_connector) {
            return _categories.FirstOrDefault(it => it.Id == id);
        }
    }
    public ICategory? GetByName(string name) {
        lock (_connector) {
            return _categories.FirstOrDefault(it => it.Name == name);
        }
    }

    public ICategory Add(string name, int sortOrder = 0, uint color = 0, string iconPath = "") {
        lock (_connector) {
            var entry = new CategoryEntry() {
                Name = name,
                SortOrder = sortOrder,
                Color = color,
                IconPath = iconPath
            };
            _categories.Add(entry);
            _connector.SaveChanges();   // insertして Idを発行させる
            return entry;
        }
    }
    public void Remove(ICategory entry) {
        lock (_connector) {
            _categories.Remove(CategoryEntry.FromCategory(entry));
        }
    }
    public void Remove(long id) {
        lock (_connector) {
            var entry = Get(id);
            if (entry != null) {
                _categories.Remove(CategoryEntry.FromCategory(entry));
            }
        }
    }
    public void Remove(string name) {
        lock (_connector) {
            var entry = GetByName(name);
            if (entry != null) {
                _categories.Remove(CategoryEntry.FromCategory(entry));
            }
        }
    }

}