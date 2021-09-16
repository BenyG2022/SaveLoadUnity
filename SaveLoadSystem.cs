using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Reflection;

//list of objects to save -> function ADD wich makes new object saveble. And functions SAVE and LOAD.
//If change serialized members than cant use load function.
namespace SaveLoadSystem
{

    public static class SaveLoad
    {
        private const string NAME_EXTENSION = ".SAV";
        private const string NAME_CORE = "/Save";
        private const int MAX_NUMBER_OF_SAVES = 100;
        private static DataToSave _dataToSave;

        private static int _savesCount = 0;
        public static int SavesCount { get { return _savesCount; } }

        private static List<object> _owners = new List<object>();
        private static List<string> _membersNames = new List<string>();
        private static List<int> _indexesOwners = new List<int>();

        private const BindingFlags INTIAL_BINDING_FLAG = BindingFlags.Instance | BindingFlags.Public;


        static SaveLoad()
        {
            for (int i = 0; i < MAX_NUMBER_OF_SAVES; i++)
            {
                if (null == LoadFile(i))
                {
                    _savesCount = i;
                    break;
                }
            }
        }

        public static void QuickSave()
        {
            //if quick save dont exist make one and save it.
            SaveFile(MAX_NUMBER_OF_SAVES);

        }
        public static void QuickLoad()
        {
            //if quick save dont exist log error.
            LoadMembers(MAX_NUMBER_OF_SAVES);
        }

        
        public static void SaveWithIndex(int index)
        {
            if (index < 0)
            {
                Debug.LogError("Save Index out of range. Must be greater than zero.");
                return;
            }
            if (index > _savesCount)
            {
                //Debug.LogWarning("Save file with index " + index + " exists. Use SaveOverride method to override existing save file. Now saving in new file.");
                Debug.LogWarning("Index greater than saves count. Changing index to " + _savesCount + " .");
                NewSave();
            }
            else if (index == _savesCount)
            {
                SaveFile(index);
                _savesCount++;
            }
            else
            {
                Debug.LogWarning("Overriding existing file with index " + index + " .");
                SaveFile(index);
            }
        }
        public static void LoadWithIndex(int index)
        {
            LoadMembers(index);
        }


        /// <summary>
        /// Serializes class member to be saved or load to/from file. ALways use before save and load.
        /// </summary>
        /// <typeparam name="T">Name of owner class.</typeparam>
        /// <param name="owner">Class object that owns member to be serialized.</param>
        /// <param name="memberName">C# member string name.</param>
        public static void SerializeMember(this object owner, string memberName)
        {
            if (_dataToSave == null)
            {
                _dataToSave = new DataToSave();
            }


            if (!_owners.Contains(owner))
            {
                _owners.Add(owner);
            }

            Type ownerType = owner.GetType() as Type;


            if (ownerType == typeof(Transform))
            {
                if (memberName == ("position"))
                {
                    PopulateLists(3, owner, memberName);
                }
                else if (memberName == ("rotation"))
                {
                    PopulateLists(4, owner, memberName);
                }
                else
                {
                    Debug.LogError("Can't serialize this member. Owner type is not recognized: + " + ownerType.FullName + " .");
                    return;
                }
            }
            else if (ownerType.IsClass)
            {
                BindingFlags flags = INTIAL_BINDING_FLAG;
                MemberInfo info = (ownerType.GetField(memberName, flags) != null) ? ownerType.GetField(memberName, flags) as MemberInfo : ownerType.GetProperty(memberName, flags) as MemberInfo;
                if (info == null)
                {
                    flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    info = (ownerType.GetField(memberName, flags) != null) ? ownerType.GetField(memberName, flags) as MemberInfo : ownerType.GetProperty(memberName, flags) as MemberInfo;
                }


                Type memberType;
                if (info.MemberType == MemberTypes.Field)
                {
                    memberType = ((FieldInfo)info).FieldType;
                }
                else if (info.MemberType == MemberTypes.Property)
                {
                    memberType = ((PropertyInfo)info).PropertyType;
                }
                else
                {
                    Debug.LogError("Can't serialize this member.Only fields and properties can be serialized, and this is " + info.MemberType + " .");
                    return;
                }

                if (memberType == typeof(Vector3))
                {
                    PopulateLists(3, owner, memberName);
                }
                else if (memberType == typeof(Vector2))
                {
                    PopulateLists(2, owner, memberName);

                }
                else if (memberType == typeof(Quaternion))
                {
                    PopulateLists(4, owner, memberName);

                }
                else if (memberType.IsPrimitive)
                {
                    PopulateLists(1, owner, memberName);

                }
                else if (memberType.IsEnum)
                {
                    PopulateLists(1, owner, memberName);
                }
                else
                {
                    Debug.LogError("Can't serialize member " + memberName + ". Wrong type " + memberType + " .");
                    return;
                }
            }
            else
            {
                Debug.LogError("Unrecognized owner type " + ownerType + " .");
            }
        }


        private static void PopulateLists(int howManyTimes, object owner, string name)
        {
            for (int i = 0; i < howManyTimes; i++)
            {
                _indexesOwners.Add(_owners.FindIndex(x => x == owner));
                _membersNames.Add(name);
            }
        }




        private static void NewSave()
        {
            SaveFile(_savesCount);
            _savesCount++;
        }



        private static void LoadMembers(int fileIndex)
        {
            DataToSave data = LoadFile(fileIndex);

            if (data == null)
            {
                return;
            }
            if (data.Values.Count != _membersNames.Count)
            {
                Debug.LogError("Can't load from file. There is no same number of values and members.");
                return;
            }

            List<object> values = new List<object>();
            values = data.Values;

            for (int i = 0; i < values.Count; i++)
            {
                object owner = _owners[_indexesOwners[i]] as object;
                Type ownerType = owner.GetType() as Type;



                if (ownerType == typeof(Transform))
                {
                    if (_membersNames[i] == ("position"))
                    {
                        ownerType.GetProperty(_membersNames[i]).SetValue(owner, new Vector3((float)values[i], (float)values[i + 1], (float)values[i + 2]));
                        i += 2;
                    }
                    else if (_membersNames[i] == ("rotation"))
                    {
                        ownerType.GetProperty(_membersNames[i]).SetValue(owner, new Quaternion((float)values[i], (float)values[i + 1], (float)values[i + 2], (float)values[i + 3]));
                        i += 3;
                    }
                    else
                    {
                        Debug.LogError("Can't unserialize this member. Owner type is not recognized: + " + ownerType.FullName + " .");
                        continue;
                    }
                }
                else if (ownerType.IsClass)
                {
                    BindingFlags flags = INTIAL_BINDING_FLAG;
                    MemberInfo info = (ownerType.GetField(_membersNames[i], flags) != null) ? ownerType.GetField(_membersNames[i], flags) as MemberInfo : ownerType.GetProperty(_membersNames[i], flags) as MemberInfo;
                    if (info == null)
                    {
                        flags = BindingFlags.NonPublic | BindingFlags.Instance;
                        info = (ownerType.GetField(_membersNames[i], flags) != null) ? ownerType.GetField(_membersNames[i], flags) as MemberInfo : ownerType.GetProperty(_membersNames[i], flags) as MemberInfo;
                    }


                    Type memberType;
                    MemberTypes fieldProperty;
                    if (info.MemberType == MemberTypes.Field)
                    {
                        memberType = ((FieldInfo)info).FieldType;
                        fieldProperty = MemberTypes.Field;
                    }
                    else if (info.MemberType == MemberTypes.Property)
                    {
                        memberType = ((PropertyInfo)info).PropertyType;
                        fieldProperty = MemberTypes.Property;
                    }
                    else
                    {
                        Debug.LogError("Can't unserialize this member. Only fields and properties can be unserialized, and this is " + info.MemberType + " .");
                        continue;
                    }

                    if (memberType == typeof(Vector3))
                    {
                        if (fieldProperty == MemberTypes.Field)
                        {
                            ownerType.GetField(_membersNames[i], flags).SetValue(owner, new Vector3((float)values[i], (float)values[i + 1], (float)values[i + 2]));
                            i += 2;
                        }
                        else if (fieldProperty == MemberTypes.Property)
                        {
                            ownerType.GetProperty(_membersNames[i], flags).SetValue(owner, new Vector3((float)values[i], (float)values[i + 1], (float)values[i + 2]));
                            i += 2;
                        }
                        else
                        {
                            Debug.LogError("Member is not field or property.");
                        }
                    }
                    else if (memberType == typeof(Vector2))
                    {
                        if (fieldProperty == MemberTypes.Field)
                        {
                            ownerType.GetField(_membersNames[i], flags).SetValue(owner, new Vector3((float)values[i], (float)values[i + 1]));
                            i++;
                        }
                        else if (fieldProperty == MemberTypes.Property)
                        {
                            ownerType.GetProperty(_membersNames[i], flags).SetValue(owner, new Vector3((float)values[i], (float)values[i + 1]));
                            i++;
                        }
                        else
                        {
                            Debug.LogError("Member is not field or property.");
                        }

                    }
                    else if (memberType == typeof(Quaternion))
                    {
                        if (fieldProperty == MemberTypes.Field)
                        {
                            ownerType.GetField(_membersNames[i], flags).SetValue(owner, new Quaternion((float)values[i], (float)values[i + 1], (float)values[i + 2], (float)values[i + 3]));
                            i += 3;
                        }
                        else if (fieldProperty == MemberTypes.Property)
                        {
                            ownerType.GetProperty(_membersNames[i], flags).SetValue(owner, new Quaternion((float)values[i], (float)values[i + 1], (float)values[i + 2], (float)values[i + 3]));
                            i += 3;
                        }
                        else
                        {
                            Debug.LogError("Member is not field or property.");
                        }

                    }
                    else if (memberType.IsPrimitive || memberType.IsEnum)
                    {
                        if (fieldProperty == MemberTypes.Field)
                        {
                            ownerType.GetField(_membersNames[i], flags).SetValue(owner, values[i]);
                        }
                        else if (fieldProperty == MemberTypes.Property)
                        {
                            ownerType.GetProperty(_membersNames[i], flags).SetValue(owner, values[i]);

                        }
                        else
                        {
                            Debug.LogError("Member is not field or property.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Can't unserialize member " + _membersNames[i] + ". Wrong type " + memberType + " .");
                        continue;
                    }
                }
                else
                {
                    Debug.LogError("Unrecognized owner type " + ownerType + " .");
                }
            }
        }


        private static DataToSave LoadFile(int index)
        {
            string path = Application.persistentDataPath + NAME_CORE + index + NAME_EXTENSION;
            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);
                DataToSave data = formatter.Deserialize(stream) as DataToSave;
                stream.Close();
                return data;

            }
            else
            {
                Debug.LogWarning("Save file not found in " + path + " .");
                return null;
            }
        }

        private static void SaveFile(int index)
        {
            List<object> values = new List<object>();

            for (int i = 0; i < _membersNames.Count; i++)
            {
                object owner = _owners[_indexesOwners[i]] as object;
                Type ownerType = owner.GetType() as Type;


                if (ownerType == typeof(Transform))
                {
                    if (_membersNames[i] == ("position"))
                    {
                        Vector3 position = (Vector3)ownerType.GetProperty(_membersNames[i]).GetValue(owner);
                        values.Add(position.x);
                        values.Add(position.y);
                        values.Add(position.z);
                        i += 2;
                        continue;
                    }
                    else if (_membersNames[i] == ("rotation"))
                    {
                        Quaternion rotation = (Quaternion)ownerType.GetProperty(_membersNames[i]).GetValue(owner);
                        values.Add(rotation.x);
                        values.Add(rotation.y);
                        values.Add(rotation.z);
                        values.Add(rotation.w);
                        i += 3;
                        continue;
                    }
                    else
                    {
                        Debug.LogError("Only 'transform.position' or 'transform.rotation' can be serialized.");
                        return;
                    }
                }
                else if (ownerType.IsClass)
                {

                    BindingFlags flags = INTIAL_BINDING_FLAG;
                    MemberInfo info = (ownerType.GetField(_membersNames[i], flags) != null) ? ownerType.GetField(_membersNames[i], flags) as MemberInfo : ownerType.GetProperty(_membersNames[i], flags) as MemberInfo;
                    if (info == null)
                    {
                        flags = BindingFlags.NonPublic | BindingFlags.Instance;
                        info = (ownerType.GetField(_membersNames[i], flags) != null) ? ownerType.GetField(_membersNames[i], flags) as MemberInfo : ownerType.GetProperty(_membersNames[i], flags) as MemberInfo;
                    }

                    Type memberType;
                    MemberTypes memberFieldProperty;
                    if (info.MemberType == MemberTypes.Field)
                    {
                        memberType = ((FieldInfo)info).FieldType;
                        memberFieldProperty = MemberTypes.Field;
                    }
                    else if (info.MemberType == MemberTypes.Property)
                    {
                        memberType = ((PropertyInfo)info).PropertyType;
                        memberFieldProperty = MemberTypes.Property;
                    }
                    else
                    {
                        Debug.LogError("Save can't be done unapproperiate member type " + info.MemberType + " .");
                        return;
                    }


                    if (memberType == typeof(Vector3))
                    {
                        Vector3 vec = GetMemberValue<Vector3>(owner, memberFieldProperty, _membersNames[i], flags);
                        values.Add(vec.x);
                        values.Add(vec.y);
                        values.Add(vec.z);
                        i += 2;
                    }
                    else if (memberType == typeof(Vector2))
                    {
                        Vector2 vec = GetMemberValue<Vector2>(owner, memberFieldProperty, _membersNames[i], flags);
                        values.Add(vec.x);
                        values.Add(vec.y);
                        i++;
                    }
                    else if (memberType == typeof(Quaternion))
                    {
                        Quaternion rot = GetMemberValue<Quaternion>(owner, memberFieldProperty, _membersNames[i], flags);
                        values.Add(rot.x);
                        values.Add(rot.y);
                        values.Add(rot.z);
                        values.Add(rot.w);
                        i += 3;
                    }
                    else if (memberType.IsPrimitive)
                    {
                        values.Add(GetMemberValue<object>(owner, memberFieldProperty, _membersNames[i], flags));
                    }
                    else if (memberType.IsEnum)
                    {
                        values.Add(GetMemberValue<object>(owner, memberFieldProperty, _membersNames[i], flags));
                    }
                    else
                    {
                        Debug.LogError("Can't serialize this member. Wrong type " + memberType + " .");
                        return;
                    }

                }
                else
                {
                    Debug.LogError("Can't save file. OwnerType is not supported. - " + ownerType);
                    return;
                }
            }

            _dataToSave.Values = values;

            BinaryFormatter formatter = new BinaryFormatter();
            string path = Application.persistentDataPath + NAME_CORE + index + NAME_EXTENSION;
            FileStream stream = new FileStream(path, FileMode.Create);
            formatter.Serialize(stream, _dataToSave);
            stream.Close();
        }


        private static T GetMemberValue<T>(object owner, MemberTypes type, string name, BindingFlags flags)
        {
            if (type == MemberTypes.Property)
            {
                return (T)owner.GetType().GetProperty(name, flags).GetValue(owner);
            }
            else if (type == MemberTypes.Field)
            {
                return (T)owner.GetType().GetField(name, flags).GetValue(owner);
            }
            else
            {
                Debug.LogError("Not field or either a property, this is " + type + " .");
                return default(T);
            }
        }

        public static void SaveScreenShot(int index)
        {
            ScreenCapture.CaptureScreenshot(Application.persistentDataPath + NAME_CORE + index + ".png");
        }
        public static Texture2D LoadScreenShot(int index)
        {
            return LoadPNG(Application.persistentDataPath + NAME_CORE + index + ".png");
        }

        private static Texture2D LoadPNG(string filePath)
        {

            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }
            return tex;
        }
    }










    /// <summary>
    /// Class stores list of indexes and values. Indexes points on owners and values are listed in same order as indexes and names.
    /// </summary>
    [System.Serializable]
    public class DataToSave
    {
        // private List<string> _names = new List<string>();

        private List<object> _values = new List<object>();
        //private List<int> _indexes = new List<int>();
        public List<object> Values { get { return _values; } set { _values = value; } }
    }
}
