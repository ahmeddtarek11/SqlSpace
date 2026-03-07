using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using SqlSpace.Application.Abstractions.Access;

namespace SqlSpace.Api.Controllers.AccessControl.Dtos;

public sealed class GrantAccessRequest
{
   
    [Required]
    [EmailAddress]
    public string TargetUserEmail { get; set; } = string.Empty;

    
    public bool HasFullAccess { get; set; }

    [Description("Optional restricted tables list. Required when HasFullAccess=false.")]
    public List<TableRestrictionInput>? RestrictedTables { get; set; }
}
