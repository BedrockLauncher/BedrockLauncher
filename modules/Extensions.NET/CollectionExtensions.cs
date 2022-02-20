using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JemExtensions
{
    public static class CollectionExtensions
    {
        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);

            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }

        public static int FindIndex<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int i = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                    return i;
                i++;
            }
            return -1;
        }

        public static int FindIndex<T>(this IEnumerable<T> source, Func<T, bool> predicate, int start)
        {
            var index = source.Skip(start).FindIndex(predicate);
            return index == -1 ? -1 : index + start;
        }


        public static bool Exists<T>(this ObservableCollection<T> coll, Predicate<T> match)
        {
            return coll.ToList().Exists(match);
        }


        public static int RemoveAll<T>(this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }
    }
}
