namespace Ims.Api.Features.Permissions;

internal static class AccessConventionExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params string[] anyOf)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new PermissionRequirement(anyOf));

    public static TBuilder RequireModule<TBuilder>(this TBuilder builder, string module)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new ModuleRequirement(module));

    public static TBuilder AuthenticatedOnly<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AuthenticatedOnly());

    public static TBuilder RequireLookup<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new PermissionRequirement([PermissionSnapshot.AnyScreen]));
}
