using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;


namespace JsonLibs
{
    public class MyJsonLib : Hashtable, IDisposable
    {
        #region Disposable
        private bool disposed;
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.disposeAll();
                }
                this.disposed = true;
            }
        }
        ~MyJsonLib()
        {
            this.Dispose(false);
        }
        #endregion

        private void disposeAll()
        {
            // disini dispose semua yang bisa di dispose
            // dispose semua array yang terkandung didalam root json dan semua subjson nya
            foreach (DictionaryEntry de in this)
            {
                if (de.Value == null) continue;
                if (de.Value.GetType().Equals(typeof(MyJsonArray)))
                {
                    ((MyJsonArray)de.Value).Dispose();
                    try
                    {
                        ((MyJsonArray)de.Value).Dispose();
                    }
                    catch { }
                }
                if (de.Value.GetType().Equals(typeof(MyJsonLib)))
                {
                    try
                    {
                        ((MyJsonLib)de.Value).Dispose();
                    }
                    catch { }
                }
            }
        }

        string MapClrMemberNameToJsonFieldName(string clrPropertyName)
        {
            return clrPropertyName;
        }

        bool isObjectTypeValid(object de)
        {
            return true;

            if (de == null) return true;
            if ((de.GetType().Equals(typeof(System.String))) ||
                (de.GetType().Equals(typeof(System.DateTime))) ||
                (de.GetType().Equals(typeof(System.Int32))) ||
                (de.GetType().Equals(typeof(System.Int64))) ||
                (de.GetType().Equals(typeof(System.Boolean))) ||
                (de.GetType().Equals(typeof(System.Single))) ||
                (de.GetType().Equals(typeof(System.Double))) ||
                (de.GetType().Equals(typeof(System.Decimal))) ||
                (de.GetType().Equals(typeof(MyJsonLib))) ||
                (de.GetType().Equals(typeof(MyJsonArray))))
            {
                return true;
            }
            else return false;
        }

        public override void Add(object key, object value)
        {
            if (this.ContainsKey(key)) this.Remove(key);
            if (!key.GetType().Equals(typeof(System.String)))
            {
                string skey = key.ToString();
                if (isObjectTypeValid(value))
                    base.Add(skey, value);
                else
                    base.Add(skey, value.GetType().ToString());
            }
            else
            {
                if (isObjectTypeValid(value))
                    base.Add(key, value);
                else
                    base.Add(key, value.GetType().ToString());
            }
        }
        public void Add(string key, object value)
        {
            if (this.ContainsKey(key)) this.Remove(key);
            if (isObjectTypeValid(value))
                base.Add(key, value);
            else
                base.Add(key, value.GetType().ToString());
        }

        public bool AddArrayItem(string key, object value)
        {
            if (!isObjectTypeValid(value)) return false;
            if (this.ContainsKey(key))
            {
                if (!this[key].GetType().Equals(typeof(MyJsonArray))) return false;
                ((MyJsonArray)this[key]).Add(value);
                return true;
            }
            else
            {
                MyJsonArray anArray = new MyJsonArray();
                anArray.Add(value);
                this.Add(key, anArray);
                return true;
            }
        }

        public bool isArray(string key)
        {
            if (!this.ContainsKey(key)) return false;
            if (this[key].GetType().Equals(typeof(MyJsonArray)))
                return true;
            else
                return false;
        }

        public bool isNull(string key)
        {
            if (!this.ContainsKey(key)) return true;
            if (this[key] == null)
                return true;
            else
                return false;
        }

        public bool isExists(string key)
        {
            return this.ContainsKey(key);
        }

        public bool isInt(string key)
        {
            return isInt32(key);
        }

        public bool isInt32(string key)
        {
            if (!this.ContainsKey(key)) return false;
            if (this[key].GetType().Equals(typeof(System.Int32)))
                return true;
            else
                return false;
        }

        public bool isInt64(string key)
        {
            if (!this.ContainsKey(key)) return false;
            if (this[key].GetType().Equals(typeof(System.Int64)))
                return true;
            else
                return false;
        }

        public bool isString(string key)
        {
            if (!this.ContainsKey(key)) return false;
            if (this[key].GetType().Equals(typeof(System.String)))
                return true;
            else
                return false;
        }

        public bool isJson(string key)
        {
            if (!this.ContainsKey(key)) return false;
            if (this[key].GetType().Equals(typeof(MyJsonLib)))
                return true;
            else
                return false;
        }

        //public override object this[object key]
        //{
        //    get
        //    { // get implementation 
        //        return base[key];
        //    }
        //    set
        //    { // set implementation 
        //        if ((de.Value.GetType().Equals(typeof(System.Int32))) ||
        //            (de.Value.GetType().Equals(typeof(System.Int64))) ||
        //            (de.Value.GetType().Equals(typeof(System.Boolean))) ||
        //            (de.Value.GetType().Equals(typeof(System.Double))))
        //        {
        //        }
        //    }
        //}

        /// <summary>
        /// baru support 1 layer data json dengan tipe string, int, boolean dan double
        /// </summary>
        /// <param name="jsonItems"></param>
        /// <returns></returns>
        public string JSONConstruct()
        {
            return ConstructObject();
        }

        private string ConstructObject()
        {
            string json = "{";
            string fieldName = "";
            string sFieldValue = "";
            //object fieldValue = null;
            //Console.WriteLine("jumlah: " + jsonItems.Count.ToString());
            bool awal = true;
            foreach (DictionaryEntry de in this)
            {
                //object dd = de.Value.GetType();
                if (!awal) { json += ","; }
                awal = false;
                fieldName = de.Key as string;
                if (de.Value == null)
                {
                    json += "\"" + fieldName + "\":null";
                }
                else if (de.Value.GetType().Equals(typeof(System.String)))
                {
                    sFieldValue = de.Value.ToString();
                    if (sFieldValue.Length == 0)
                    {
                        // string
                        json += "\"" + fieldName + "\":\"\"";
                    }
                    else if ((sFieldValue[0] == '{') && (sFieldValue[sFieldValue.Length - 1] == '}'))
                    {
                        // sub json
                        json += "\"" + fieldName + "\":" + de.Value.ToString();
                    }
                    else if ((sFieldValue[0] == '[') && (sFieldValue[sFieldValue.Length - 1] == ']'))
                    {
                        // array
                        json += "\"" + fieldName + "\":" + de.Value.ToString();
                    }
                    else
                    {
                        // string
                        json += "\"" + fieldName + "\":\"" + de.Value.ToString() + "\"";
                    }

                }
                else if ((de.Value.GetType().Equals(typeof(System.DateTime))))
                {
                    json += "\"" + fieldName + "\":\"" + ((System.DateTime)de.Value).ToString("yyyy-MM-dd HH:mm:ss") + "\"";
                }
                else if ((de.Value.GetType().Equals(typeof(System.Int32))) ||
                    (de.Value.GetType().Equals(typeof(System.Int64))) ||
                    (de.Value.GetType().Equals(typeof(System.Boolean))) ||
                    (de.Value.GetType().Equals(typeof(System.Decimal))) ||
                    (de.Value.GetType().Equals(typeof(System.Single))) ||
                    (de.Value.GetType().Equals(typeof(System.Double))))
                {
                    json += "\"" + fieldName + "\":" + de.Value.ToString().ToLower();
                }
                else if (de.Value.GetType().Equals(typeof(JsonLibs.MyJsonLib)))
                {
                    json += "\"" + fieldName + "\":" +
                        ((MyJsonLib)de.Value).ConstructObject();
                }
                else if (de.Value.GetType().Equals(typeof(JsonLibs.MyJsonArray)))
                {
                    json += "\"" + fieldName + "\":" +
                        ((MyJsonArray)de.Value).Construct();
                }
                //Console.WriteLine("Type: " + de.Value.GetType().ToString());
                //Console.WriteLine(fieldName);
                //fieldValue = de.Value;
                //Console.WriteLine(fieldValue.ToString());
            }

            json += "}";

            //var fieldNames = typeof(MyJsonLib).GetFields()
            //                .Select(field => field.Name)
            //                .ToList();
            return json;
        }


        private const int TOKEN_NONE = 0;
        private const int TOKEN_CURLY_OPEN = 1;
        private const int TOKEN_CURLY_CLOSE = 2;
        private const int TOKEN_SQUARED_OPEN = 3;
        private const int TOKEN_SQUARED_CLOSE = 4;
        private const int TOKEN_COLON = 5;
        private const int TOKEN_COMMA = 6;
        private const int TOKEN_STRING = 7;
        private const int TOKEN_NUMBER = 8;
        private const int TOKEN_TRUE = 9;
        private const int TOKEN_FALSE = 10;
        private const int TOKEN_NULL = 11;
        private const int BUILDER_CAPACITY = 2000;

        public string Name = "";
        public MyJsonLib Parent = null;

        // hasil disimpan di internal hashtable
        //public MyJsonLib2 result;
        public bool JSONParse(string json)
        {
            bool success = true;
            if (json != null)
            {
                char[] charArray = json.ToCharArray();
                int index = 0;
                object obj;
                EatWhitespace(charArray, ref index);

                if ((charArray == null) || (charArray.Length == 0) || (charArray[index] != '{'))
                    return false;

                //obj = ParseValue(charArray, ref index, ref success);
                obj = ParseObject(charArray, ref index, ref success, false);
                //Console.WriteLine(typeof(MyJsonLib2).ToString() + " " + obj.GetType().ToString());
                //if (obj == null) return false;
                //result = (MyJsonLib2)obj;
                return (bool)obj;
            }
            //else
            //    obj = null;

            return success;
        }

        object ParseValue(char[] json, ref int index, ref bool success)
        {
            switch (LookAhead(json, index))
            {
                case TOKEN_STRING:
                    return ParseString(json, ref index, ref success);
                case TOKEN_NUMBER:
                    return ParseNumber(json, ref index, ref success);
                case TOKEN_CURLY_OPEN:
                    return ParseObject(json, ref index, ref success, true);
                case TOKEN_SQUARED_OPEN:
                    return ParseArray(json, ref index, ref success);
                case TOKEN_TRUE:
                    NextToken(json, ref index);
                    return true;
                case TOKEN_FALSE:
                    NextToken(json, ref index);
                    return false;
                case TOKEN_NULL:
                    NextToken(json, ref index);
                    return null;
                case TOKEN_NONE:
                    break;
            }
            success = false;
            return null;
        }

        object ParseObject(char[] json, ref int index, ref bool success, bool fSubJson)
        {
            //            IDictionary<string, object> table = new JsonObject();
            MyJsonLib table;
            if (fSubJson)
            {
                table = new MyJsonLib();
                //                table.Parent = this;
            }
            else table = this;

            int token;

            // {
            NextToken(json, ref index);

            while (true)
            {
                token = LookAhead(json, index);
                if (token == TOKEN_NONE)
                {
                    success = false;
                    if (fSubJson) return null; else return false;
                }
                else if (token == TOKEN_COMMA)
                    NextToken(json, ref index);
                else if (token == TOKEN_CURLY_CLOSE)
                {
                    NextToken(json, ref index);
                    if (fSubJson) return table; else return true;
                }
                else
                {
                    // name
                    string name = ParseString(json, ref index, ref success);
                    if (!success)
                    {
                        success = false;
                        if (fSubJson) return null; else return false;
                    }
                    // :
                    token = NextToken(json, ref index);
                    if (token != TOKEN_COLON)
                    {
                        success = false;
                        if (fSubJson) return null; else return false;
                    }
                    // value
                    object value = ParseValue(json, ref index, ref success);
                    if (!success)
                    {
                        success = false;
                        if (fSubJson) return null; else return false;
                    }
                    if ((value != null) && (value.GetType() == typeof(MyJsonLib)))
                    {
                        ((MyJsonLib)value).Name = name;
                        ((MyJsonLib)value).Parent = this;
                    }
                    if ((value != null) && (value.GetType() == typeof(MyJsonArray)))
                    {
                        ((MyJsonArray)value).Name = name;
                        ((MyJsonArray)value).Parent = this;
                    }
                    table.Add(name, value);
                    //table[name] = value;
                }
            }
            //if (fSubJson) return null; else return false;
            //return table;
        }

        MyJsonArray ParseArray(char[] json, ref int index, ref bool success)
        {
            MyJsonArray array = new MyJsonArray();

            // [
            NextToken(json, ref index);

            bool done = false;
            while (!done)
            {
                int token = LookAhead(json, index);
                if (token == TOKEN_NONE)
                {
                    success = false;
                    return null;
                }
                else if (token == TOKEN_COMMA)
                    NextToken(json, ref index);
                else if (token == TOKEN_SQUARED_CLOSE)
                {
                    NextToken(json, ref index);
                    break;
                }
                else
                {
                    object value = ParseValue(json, ref index, ref success);
                    if (!success)
                        return null;
                    array.Add(value);
                }
            }
            return array;
        }

        string ParseString(char[] json, ref int index, ref bool success)
        {
            StringBuilder s = new StringBuilder(BUILDER_CAPACITY);
            char c;

            EatWhitespace(json, ref index);

            // "
            c = json[index++];
            bool complete = false;
            while (!complete)
            {
                if (index == json.Length)
                    break;

                c = json[index++];
                if (c == '"')
                {
                    complete = true;
                    break;
                }
                else if (c == '\\')
                {
                    if (index == json.Length)
                        break;
                    c = json[index++];
                    if (c == '"')
                        s.Append('"');
                    else if (c == '\\')
                        s.Append('\\');
                    else if (c == '/')
                        s.Append('/');
                    else if (c == 'b')
                        s.Append('\b');
                    else if (c == 'f')
                        s.Append('\f');
                    else if (c == 'n')
                        s.Append('\n');
                    else if (c == 'r')
                        s.Append('\r');
                    else if (c == 't')
                        s.Append('\t');
                    else if (c == 'u')
                    {
                        int remainingLength = json.Length - index;
                        if (remainingLength >= 4)
                        {
                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint;
                            if (!(success = UInt32.TryParse(new string(json, index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint)))
                                return "";

                            // convert the integer codepoint to a unicode char and add to string
                            if (0xD800 <= codePoint && codePoint <= 0xDBFF)  // if high surrogate
                            {
                                index += 4; // skip 4 chars
                                remainingLength = json.Length - index;
                                if (remainingLength >= 6)
                                {
                                    uint lowCodePoint;
                                    if (new string(json, index, 2) == "\\u" && UInt32.TryParse(new string(json, index + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out lowCodePoint))
                                    {
                                        if (0xDC00 <= lowCodePoint && lowCodePoint <= 0xDFFF)    // if low surrogate
                                        {
                                            s.Append((char)codePoint);
                                            s.Append((char)lowCodePoint);
                                            index += 6; // skip 6 chars
                                            continue;
                                        }
                                    }
                                }
                                success = false;    // invalid surrogate pair
                                return "";
                            }
                            s.Append(ConvertFromUtf32((int)codePoint));
                            // skip 4 chars
                            index += 4;
                        }
                        else
                            break;
                    }
                }
                else
                    s.Append(c);
            }
            if (!complete)
            {
                success = false;
                return null;
            }
            return s.ToString();
        }

        private string ConvertFromUtf32(int utf32)
        {
            // http://www.java2s.com/Open-Source/CSharp/2.6.4-mono-.net-core/System/System/Char.cs.htm
            if (utf32 < 0 || utf32 > 0x10FFFF)
                throw new ArgumentOutOfRangeException("utf32", "The argument must be from 0 to 0x10FFFF.");
            if (0xD800 <= utf32 && utf32 <= 0xDFFF)
                throw new ArgumentOutOfRangeException("utf32", "The argument must not be in surrogate pair range.");
            if (utf32 < 0x10000)
                return new string((char)utf32, 1);
            utf32 -= 0x10000;
            return new string(new char[] { (char)((utf32 >> 10) + 0xD800), (char)(utf32 % 0x0400 + 0xDC00) });
        }

        object ParseNumber(char[] json, ref int index, ref bool success)
        {
            EatWhitespace(json, ref index);
            int lastIndex = GetLastIndexOfNumber(json, index);
            int charLength = (lastIndex - index) + 1;
            object returnNumber;
            string str = new string(json, index, charLength);
            if (str.IndexOf(".", StringComparison.OrdinalIgnoreCase) != -1 || str.IndexOf("e", StringComparison.OrdinalIgnoreCase) != -1)
            {
                double number;
                success = double.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
                returnNumber = number;
            }
            else
            {
                long number;
                success = long.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
                if (number <= Int32.MaxValue)
                {
                    returnNumber = Convert.ToInt32(number);
                }
                else
                    returnNumber = number;
            }
            index = lastIndex + 1;
            return returnNumber;
        }

        int GetLastIndexOfNumber(char[] json, int index)
        {
            int lastIndex;
            for (lastIndex = index; lastIndex < json.Length; lastIndex++)
                if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1) break;
            return lastIndex - 1;
        }

        void EatWhitespace(char[] json, ref int index)
        {
            for (; index < json.Length; index++)
                if (" \t\n\r\b\f".IndexOf(json[index]) == -1) break;
        }

        int LookAhead(char[] json, int index)
        {
            int saveIndex = index;
            return NextToken(json, ref saveIndex);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        int NextToken(char[] json, ref int index)
        {
            EatWhitespace(json, ref index);
            if (index == json.Length)
                return TOKEN_NONE;
            char c = json[index];
            index++;
            switch (c)
            {
                case '{':
                    return TOKEN_CURLY_OPEN;
                case '}':
                    return TOKEN_CURLY_CLOSE;
                case '[':
                    return TOKEN_SQUARED_OPEN;
                case ']':
                    return TOKEN_SQUARED_CLOSE;
                case ',':
                    return TOKEN_COMMA;
                case '"':
                    return TOKEN_STRING;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    return TOKEN_NUMBER;
                case ':':
                    return TOKEN_COLON;
            }
            index--;
            int remainingLength = json.Length - index;
            // false
            if (remainingLength >= 5)
            {

                if (char.ToLower(json[index]) == 'f' && char.ToLower(json[index + 1]) == 'a' &&
                    char.ToLower(json[index + 2]) == 'l' && char.ToLower(json[index + 3]) == 's' &&
                    char.ToLower(json[index + 4]) == 'e')
                {
                    index += 5;
                    return TOKEN_FALSE;
                }
            }
            // true
            if (remainingLength >= 4)
            {
                if (char.ToLower(json[index]) == 't' && char.ToLower(json[index + 1]) == 'r' &&
                    char.ToLower(json[index + 2]) == 'u' && char.ToLower(json[index + 3]) == 'e')
                {
                    index += 4;
                    return TOKEN_TRUE;
                }
            }
            // null
            if (remainingLength >= 4)
            {
                if (json[index] == 'n' && json[index + 1] == 'u' && json[index + 2] == 'l' && json[index + 3] == 'l')
                {
                    index += 4;
                    return TOKEN_NULL;
                }
            }
            return TOKEN_NONE;
        }

        //public IDictionary<string, object> getDictionary(object aClass)
        //{
        //    FieldInfo[] myFieldInfo;
        //    Type myType = aClass.GetType();

        //    // Get the type and fields of FieldInfoClass.
        //    myFieldInfo = myType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance
        //        | BindingFlags.Public);

        //    foreach (FieldInfo aField in myFieldInfo)
        //    {
        //        aField.GetValue(aClass);
        //    }

        //    Dictionary<string, object> variableValues =
        //        aClass.GetType().GetFields(BindingFlags.Instance |
        //                BindingFlags.Static | BindingFlags.Public)
        //            .ToDictionary(f => f.Name, f => f.GetValue(aClass));

        //    return variableValues;
        //}

        //public IDictionary<string, object> getDictionary(object aClass)
        public MyJsonLib parseClass(object aClass)
        {
            MyJsonLib aJson = new MyJsonLib();

            FieldInfo[] myFieldInfo;
            PropertyInfo[] myPropertyInfo;
            Type myType = aClass.GetType();

            // Get the type and fields of FieldInfoClass.
            myFieldInfo = myType.GetFields(BindingFlags.Instance
                | BindingFlags.Public);
            myPropertyInfo = myType.GetProperties(BindingFlags.Instance
                | BindingFlags.Public);

            foreach (PropertyInfo aProperty in myPropertyInfo)
            {
                if ((aProperty.CanRead) && (aProperty.CanWrite))
                {
                    aJson.Add(MapClrMemberNameToJsonFieldName(aProperty.Name),aProperty.GetValue(aClass,null));
                    //result[MapClrMemberNameToJsonFieldName(propertyInfo.Name)] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(propertyInfo.PropertyType, ReflectionUtils.GetSetMethod(propertyInfo));
                }
            }
            foreach (FieldInfo aField in myFieldInfo)
            {
                    aJson.Add(MapClrMemberNameToJsonFieldName(aField.Name),aField.GetValue(aClass));
//                aField.GetValue(aClass);
            }

            return aJson;
            //Dictionary<string, object> variableValues =
            //    aClass.GetType().GetFields(BindingFlags.Instance |
            //            BindingFlags.Static | BindingFlags.Public)
            //        .ToDictionary(f => f.Name, f => f.GetValue(aClass));

            //return variableValues;
        }

        //internal virtual IDictionary<string, object> 
        //{
        //    IDictionary<string, object> result = new Dictionary<string, object>();
        //    foreach (PropertyInfo propertyInfo in ReflectionUtils.GetProperties(type))
        //    {
        //        if (propertyInfo.CanWrite)
        //        {
        //            MethodInfo setMethod = ReflectionUtils.GetSetterMethodInfo(propertyInfo);
        //            if (setMethod.IsStatic || !setMethod.IsPublic)
        //                continue;
        //            result[MapClrMemberNameToJsonFieldName(propertyInfo.Name)] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(propertyInfo.PropertyType, ReflectionUtils.GetSetMethod(propertyInfo));
        //        }
        //    }
        //    foreach (FieldInfo fieldInfo in ReflectionUtils.GetFields(type))
        //    {
        //        if (fieldInfo.IsInitOnly || fieldInfo.IsStatic || !fieldInfo.IsPublic)
        //            continue;
        //        result[MapClrMemberNameToJsonFieldName(fieldInfo.Name)] = new KeyValuePair<Type, ReflectionUtils.SetDelegate>(fieldInfo.FieldType, ReflectionUtils.GetSetMethod(fieldInfo));
        //    }
        //    return result;
        //}


    }

}
