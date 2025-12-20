using Microsoft.AspNetCore.Authorization;

namespace WhatsAppAdmin.Authorization
{
    /// <summary>
    /// Authorization handler that grants SuperAdmin access to all resources
    /// SuperAdmin bypasses all authorization requirements
    /// </summary>
    public class SuperAdminAuthorizationHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            // If user is in SuperAdmin role, succeed all requirements
            if (context.User.IsInRole("SuperAdmin"))
            {
                foreach (var requirement in context.PendingRequirements.ToList())
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
