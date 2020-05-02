using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NETCore.Encrypt;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EntityFrameworkCore.Initialization.Converters
{
    public static class EncryptedConverterExtensions
    {
        public static void AddEncryptedValues(this ModelBuilder modelBuilder, string aesKey)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var attributes = property.PropertyInfo?.GetCustomAttributes(typeof(EncryptedAttribute), false);
                    if (attributes != null && attributes.Any())
                    {
                        property.SetValueConverter(new EncryptedConverter(aesKey));
                    }
                }
            }
        }

        public static void AddEncryptedValues(this ModelBuilder modelBuilder, IDataProtectionProvider dataProtectionProvider)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var attributes = property.PropertyInfo?.GetCustomAttributes(typeof(EncryptedAttribute), false);
                    if (attributes != null && attributes.Any())
                    {
                        property.SetValueConverter(new EncryptedConverter(dataProtectionProvider));
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
        class AesWrapper
        {
            readonly string _aesKey;

            public AesWrapper(string aesKey)
            {
                _aesKey = aesKey;
            }

            public Expression<Func<string, string>> To => x => x != null ? EncryptProvider.AESEncrypt(x, _aesKey) : null;
            public Expression<Func<string, string>> From => x => x != null ? EncryptProvider.AESDecrypt(x, _aesKey) : null;
        }

        public EncryptedConverter(string aesKey, ConverterMappingHints mappingHints = default)
        : this(new AesWrapper(aesKey), mappingHints)
        { }

        EncryptedConverter(AesWrapper wrapper, ConverterMappingHints mappingHints)
            : base(wrapper.To, wrapper.From, mappingHints)
        { }

        class DataProtectionWrapper
        {
            readonly IDataProtector _dataProtector;

            public DataProtectionWrapper(IDataProtectionProvider dataProtectionProvider)
            {
                _dataProtector = dataProtectionProvider.CreateProtector(nameof(EncryptedConverter));
            }

            public Expression<Func<string, string>> To => x => x != null ? _dataProtector.Protect(x) : null;
            public Expression<Func<string, string>> From => x => x != null ? _dataProtector.Unprotect(x) : null;
        }

        public EncryptedConverter(IDataProtectionProvider provider, ConverterMappingHints mappingHints = default)
        : this(new DataProtectionWrapper(provider), mappingHints)
        { }

        EncryptedConverter(DataProtectionWrapper wrapper, ConverterMappingHints mappingHints)
            : base(wrapper.To, wrapper.From, mappingHints)
        { }
    }
}

