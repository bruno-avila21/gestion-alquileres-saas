using Microsoft.AspNetCore.Authorization;

namespace GestionAlquileres.API.Controllers;

/// <summary>
/// Base for management endpoints restricted to organization staff.
/// Inherits authentication + tenant/user claim helpers from <see cref="BaseController"/>
/// and adds role authorization: only Admin and Staff may access derived controllers.
/// Tenant-facing endpoints must NOT inherit from this (use <see cref="BaseController"/> + MeController).
/// </summary>
[Authorize(Roles = "Admin,Staff")]
public abstract class AdminControllerBase : BaseController
{
}
