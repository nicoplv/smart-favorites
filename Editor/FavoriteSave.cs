using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SmartFavorites
{
    public class FavoriteSave : ScriptableObject
    {
        public int CurrentListIndex = 0;
        public List<FavoriteList> FavoriteLists = new List<FavoriteList>();
        public FavoriteList CurrentList { get { return FavoriteLists[CurrentListIndex]; } }

        public FavoriteSave()
        {
            FavoriteLists = new List<FavoriteList>();
            AddList();
        }

        public void AddList()
        {
            // Search name
            string name = "Favorites ";
            List<string> names = NameList().ToList();
            for (int i = 1; i < 1000; i++)
            {
                name = "Favorites " + i;
                if (!names.Contains(name))
                    break;
            }

            // Add new list
            FavoriteLists.Add(new FavoriteList(name));
            CurrentListIndex = FavoriteLists.Count - 1;
        }

        public void RemoveList(int _index)
        {
            if (FavoriteLists.Count > 1)
            {
                FavoriteLists.RemoveAt(_index);
                if (CurrentListIndex >= FavoriteLists.Count)
                    CurrentListIndex--;
            }
        }

        public string[] NameList()
        {
            string[] nameList = new string[FavoriteLists.Count];
            for (int i = 0; i < FavoriteLists.Count; i++)
                nameList[i] = FavoriteLists[i].Name;
            return nameList;
        }
    }

    [Serializable]
    public class FavoriteList
    {
        public string Name;
        public List<UnityEngine.Object> Objects;

        public FavoriteList(string _name = "Favorites")
        {
            Name = _name;
            Objects = new List<UnityEngine.Object>();
        }

        public void Update()
        {
            // Clean list and sort
            Objects.RemoveAll(obj => obj == null);
            Objects.Sort(delegate (UnityEngine.Object _a, UnityEngine.Object _b) { return (new CaseInsensitiveComparer()).Compare(_a.name, _b.name); });
        }

        public UnityEngine.Object Get(int _index)
        {
            if (Objects.Count < _index)
                return null;
            return Objects[_index];
        }

        public bool Contains(UnityEngine.Object _object)
        {
            return Objects.Contains(_object);
        }

        public void Add(List<UnityEngine.Object> _objects)
        {
            Objects.AddRange(_objects);
        }

        public void Add(UnityEngine.Object _object)
        {
            Objects.Add(_object);
        }
        public void Remove(List<UnityEngine.Object> _objects)
        {
            foreach (UnityEngine.Object iObject in _objects)
                Objects.Remove(iObject);
        }

        public void Remove(UnityEngine.Object _object)
        {
            Objects.Remove(_object);
        }

        public void RemoveAt(int _index)
        {
            Objects.RemoveAt(_index);
        }

        public void Clear()
        {
            Objects.Clear();
        }
    }
}