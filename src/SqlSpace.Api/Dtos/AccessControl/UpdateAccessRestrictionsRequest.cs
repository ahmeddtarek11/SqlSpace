using System.ComponentModel;
using SqlSpace.Application.Abstractions.Access;

namespace SqlSpace.Api.Controllers.AccessControl.Dtos;

public sealed class UpdateAccessRestrictionsRequest
{
   
    public bool HasFullAccess { get; set; }

   
    public List<TableRestrictionInput>? RestrictedTables { get; set; }
}
