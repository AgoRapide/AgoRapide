// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgoRapide.Database;

namespace AgoRapide.Core {

    [Class(Description = "Describes a foreign key. See also -"+ nameof(GetForeignEntity) + "-.")]
    public class ForeignKey : ITypeDescriber {

        public long Id { get; private set; }
        [ClassMember(Description = "Necessary in order for " + nameof(GetForeignEntity) + " to function")]
        private PropertyKeyAttributeEnriched Key;

        private ForeignKey(PropertyKeyAttributeEnriched key, long id) {
            Key = key ?? throw new NullReferenceException(nameof(key));
            if (key.A.ForeignKeyOf == null) throw new NullReferenceException(nameof(key.A.ForeignKeyOf) + ". Details: " + key.ToString());
            Id = id;
        }

        public static ForeignKey Parse(PropertyKeyAttributeEnriched key, string value) => TryParse(key, value, out var retval, out var errorResponse) ? retval : throw new InvalidForeignKeyException(nameof(value) + ": " + value + ", " + nameof(errorResponse) + ": " + errorResponse);
        public static bool TryParse(PropertyKeyAttributeEnriched key, string value, out ForeignKey id) => TryParse(key, value, out id, out var dummy);
        public static bool TryParse(PropertyKeyAttributeEnriched key, string value, out ForeignKey id, out string errorResponse) {
            if (!long.TryParse(value, out var lngId)) {
                id = null;
                errorResponse = "Invalid long-value";
                return false;
            }
            if (lngId <= 0) {
                id = null;
                errorResponse = "Invalid long-value. Not a positive value but " + lngId;
                return false;
            }

            id = new ForeignKey(key, lngId);
            errorResponse = null;
            return true;
        }

        public BaseEntity GetForeignEntity(BaseDatabase db) => db.GetEntityById(Id, requiredType: Key.A.ForeignKeyOf);
        public T GetForeignEntity<T>(BaseDatabase db) {
            InvalidTypeException.AssertAssignable(typeof(T), Key.A.ForeignKeyOf, null);
            return (T)(object)db.GetEntityById(Id, requiredType: Key.A.ForeignKeyOf);
        }

        public static void EnrichAttribute(PropertyKeyAttributeEnriched key) {
            if (key.A.ForeignKeyOf == null) throw new NullReferenceException(nameof(key.A.ForeignKeyOf) + ". Details: " + key.ToString());
            key.ValidatorAndParser = new Func<string, ParseResult>(value => {
                return TryParse(key, value, out var retval, out var errorResponse) ?
                    ParseResult.Create(key, retval) :
                    ParseResult.Create(errorResponse);
            });
        }

        public class InvalidForeignKeyException : ApplicationException {
            public InvalidForeignKeyException(string message) : base(message) { }
            public InvalidForeignKeyException(string message, Exception inner) : base(message, inner) { }
        }
    }
}