using DukeBox.model;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace DukeBox.DB.Accessor;

public class CategoryList : IMutableCategoryStore {
    private DBConnector _connector;
    private DbSet<CategoryEntry> _categories;
    public CategoryList(DBConnector connector) {
        _connector = connector;
        _categories = connector.Categories;
    }

    public IEnumerable<ICategory> Enumerable(bool tracking = false) {
        return _categories.OrderBy(it => it.SortOrder).Let(it => 
            tracking ? it.AsTracking() : it.AsNoTracking()
        );
    }

    public IList<ICategory> List(bool tracking = false) {
        return Enumerable(tracking).ToList();
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

    public ICategory? Add(string name, int sortOrder = 0, uint color = 0, string iconPath = "") {
        lock (_connector) {
            var current = GetByName(name);
            if (current!=null) {
                _connector._logger.Warn("CategoryList.Add: Category already exists. name={0}", name);
                return null;
            }
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