using System;
using System.Collections.Generic;
using System.Text;

namespace EntityFrameworkCore.Initialization.AnnotationProviders
{
    //public class RelationalMigrationsAnnotationsProvider : SqlServerMigrationsAnnotationProvider
    //{
    //    private readonly SqliteMigrationsAnnotationProvider _sqliteMigrationsAnnotationProvider;

    //    public RelationalMigrationsAnnotationsProvider(MigrationsAnnotationProviderDependencies dependencies)
    //       : base(dependencies)
    //    {
    //        _sqliteMigrationsAnnotationProvider = new SqliteMigrationsAnnotationProvider(dependencies);
    //    }

    //    public override IEnumerable<IAnnotation> For(IModel model)
    //    {
    //        return base.For(model).Concat(_sqliteMigrationsAnnotationProvider.For(model));
    //    }

    //    public override IEnumerable<IAnnotation> For(IProperty property)
    //    {
    //        return base.For(property).Concat(_sqliteMigrationsAnnotationProvider.For(property));
    //    }
    //}
}
