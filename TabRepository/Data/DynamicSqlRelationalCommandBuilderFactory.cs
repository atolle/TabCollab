using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Data
{
    public class DynamicSqlRelationalCommandBuilderFactory : RelationalCommandBuilderFactory
    {
        public DynamicSqlRelationalCommandBuilderFactory(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, IRelationalTypeMappingSource typeMappingSource) : base(logger, typeMappingSource)
        {
        }

        protected override IRelationalCommandBuilder CreateCore(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, IRelationalTypeMappingSource relationalTypeMappingSource)
        {
            return new DynamicSqlRelationalCommandBuilder(logger, relationalTypeMappingSource);
        }
    }
}
