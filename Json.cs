using System.Collections.Generic;


namespace Util
{
    public class JsonValue : IEnumerable<JsonValue>
    {
        public static JsonValue Parse(string src)
        {
            return ParseValue(new Parser(src));
        }


        static JsonValue ParseValue(Parser p)
        {
            p.SkipWhitespace();
            if (p.Current() == '{')
                return ParseObject(p);
            else if (p.Current() == '[')
                return ParseArray(p);
            else if (p.CurrentIsNumber())
                return MakeNumber(p.ReadNumber());
            else if (p.Current() == '"')
                return MakeString(p.ReadMultiStringLiteral());

            // Fix to avoid a sprpacker bug
            else if (p.CurrentIsIdentifier())
                return MakeString(p.ReadIdentifier());

            p.RaiseError("unexpected char");
            return null;
        }


        static JsonValue ParseObject(Parser p)
        {
            p.SkipWhitespace();
            p.Match('{');
            p.SkipWhitespace();

            var obj = MakeObject();

            while (!p.IsOver() && p.Current() != '}')
            {
                p.SkipWhitespace();

                string name;
                if (p.Current() == '"')
                    name = p.ReadStringLiteral();
                else
                    name = p.ReadIdentifier();

                p.SkipWhitespace();
                p.Match(':');
                p.SkipWhitespace();
                obj.AddField(name, ParseValue(p));
                p.SkipWhitespace();

                if (!p.TryMatch(','))
                    break;

                p.SkipWhitespace();
            }

            p.SkipWhitespace();
            p.Match('}');
            p.SkipWhitespace();

            return obj;
        }


        static JsonValue ParseArray(Parser p)
        {
            p.SkipWhitespace();
            p.Match('[');
            p.SkipWhitespace();

            var arr = MakeArray();

            while (!p.IsOver() && p.Current() != ']')
            {
                p.SkipWhitespace();
                arr.AddElement(ParseValue(p));
                p.SkipWhitespace();

                if (!p.TryMatch(','))
                    break;

                p.SkipWhitespace();
            }

            p.SkipWhitespace();
            p.Match(']');
            p.SkipWhitespace();

            return arr;
        }


        enum Kind
        {
            Object, Array, Number, String
        }

        public class Field
        {
            public string name;
            public JsonValue value;
        }

        Kind kind;
        List<Field> objectFields;
        List<JsonValue> arrayElements;
        double numberValue;
        string stringValue;


        public bool IsObject() { return kind == Kind.Object; }
        public bool IsArray() { return kind == Kind.Array; }
        public bool IsNumber() { return kind == Kind.Number; }
        public bool IsString() { return kind == Kind.String; }


        void EnsureIsObject() { if (!IsObject()) throw new System.Exception("not an object"); }
        void EnsureIsArray() { if (!IsArray()) throw new System.Exception("not an array"); }
        void EnsureIsNumber() { if (!IsNumber()) throw new System.Exception("not a number"); ; }
        void EnsureIsString() { if (!IsString()) throw new System.Exception("not a string"); }


        public void AddField(string name, JsonValue value)
        {
            EnsureIsObject();
            objectFields.Add(new Field { name = name, value = value });
        }


        public bool HasField(string fieldName)
        {
            EnsureIsObject();
            return (objectFields.Find(f => f.name == fieldName) != null);
        }


        public JsonValue TryGetField(string fieldName)
        {
            EnsureIsObject();
            var field = objectFields.Find(f => f.name == fieldName);

            if (field == null)
                return null;

            return field.value;
        }


        public int? TryGetFieldInt(string fieldName)
        {
            var field = TryGetField(fieldName);

            if (field == null)
                return null;

            return field.IntValue;
        }


        public float? TryGetFieldFloat(string fieldName)
        {
            var field = TryGetField(fieldName);

            if (field == null)
                return null;

            return field.FloatValue;
        }


        public string TryGetFieldString(string fieldName, string def = null)
        {
            var field = TryGetField(fieldName);

            if (field == null)
                return def;

            return field.StringValue;
        }


        public int TryGetFieldInt(string fieldName, int def = 0)
        {
            var field = TryGetField(fieldName);

            if (field == null)
                return def;

            return field.IntValue;
        }


        public float TryGetFieldFloat(string fieldName, float def = 0f)
        {
            var field = TryGetField(fieldName);

            if (field == null)
                return def;

            return field.FloatValue;
        }


        public List<Field> Fields
        {
            get
            {
                EnsureIsObject();
                return objectFields;
            }
        }


        public void AddElement(JsonValue element)
        {
            EnsureIsArray();
            arrayElements.Add(element);
        }


        public int Length
        {
            get
            {
                EnsureIsArray();
                return arrayElements.Count;
            }
        }


        IEnumerator<JsonValue> IEnumerable<JsonValue>.GetEnumerator()
        {
            EnsureIsArray();
            return arrayElements.GetEnumerator();
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            EnsureIsArray();
            return arrayElements.GetEnumerator();
        }


        public double DoubleValue
        {
            get
            {
                EnsureIsNumber();
                return numberValue;
            }
        }


        public float FloatValue
        {
            get
            {
                EnsureIsNumber();
                return (float)numberValue;
            }
        }


        public int IntValue
        {
            get
            {
                EnsureIsNumber();
                return (int)numberValue;
            }
        }


        public string StringValue
        {
            get
            {
                EnsureIsString();
                return stringValue;
            }
        }


        public JsonValue this[string fieldName]
        {
            get
            {
                EnsureIsObject();
                var field = objectFields.Find(f => f.name == fieldName);
                if (field == null) throw new System.Exception("field not found: " + fieldName);
                return field.value;
            }
        }


        public JsonValue this[int arrayIndex]
        {
            get
            {
                EnsureIsArray();
                if (arrayIndex < 0 || arrayIndex >= Length) throw new System.Exception("out of array bounds: " + arrayIndex);
                return arrayElements[arrayIndex];
            }
        }


        public static JsonValue MakeObject()
        {
            return new JsonValue { kind = Kind.Object, objectFields = new List<Field>() };
        }


        public static JsonValue MakeArray()
        {
            return new JsonValue { kind = Kind.Array, arrayElements = new List<JsonValue>() };
        }


        public static JsonValue MakeNumber(double value)
        {
            return new JsonValue { kind = Kind.Number, numberValue = value };
        }


        public static JsonValue MakeString(string value)
        {
            return new JsonValue { kind = Kind.String, stringValue = value };
        }
    }
}