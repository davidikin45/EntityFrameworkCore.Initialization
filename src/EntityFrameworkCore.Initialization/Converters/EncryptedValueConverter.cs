using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NETCore.Encrypt;
using System;
using System.Linq;

namespace EntityFrameworkCore.Initialization.Converters
{
    public static class EncryptedConverterExtensions
    {
        public static void AddEncryptedValues(this ModelBuilder modelBuilder, string AesKey)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var attributes = property.PropertyInfo.GetCustomAttributes(typeof(EncryptedAttribute), false);
                    if (attributes != null && attributes.Any())
                    {
                        property.SetValueConverter(new EncryptedConverter(AesKey));
                    }
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class EncryptedAttribute : Attribute
    { }

    public class EncryptedConverter : ValueConverter<string, string>
    {
        public EncryptedConverter(string AesKey, ConverterMappingHints mappingHints = default)
            : base(data => EncryptProvider.AESEncrypt(data, AesKey), data => EncryptProvider.AESDecrypt(data, AesKey), mappingHints)
        {

        }
    }
}
