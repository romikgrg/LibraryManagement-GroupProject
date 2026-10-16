using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryManagement.Services;

/// <summary>
/// How one deployment of the application knows which library it is running at.
/// </summary>
/// <remarks>
/// Each physical branch runs the same build with a different
/// <c>BranchDeployment:Code</c> setting, which is what lets the installation
/// instructions be identical everywhere except for the one line that names the
/// location. Setting it to <c>ALL</c> runs the instance in head office mode,
/// with every branch visible, which is how the manager and the development
/// machine use it.
/// </remarks>
public class BranchDeploymentOptions
{
    public const string SectionName = "BranchDeployment";

    /// <summary>
    /// The branch code this instance serves, or "ALL" for an unrestricted
    /// instance. Overridden per location by appsettings.&lt;Branch&gt;.json, an
    /// environment variable, or the branch git branch.
    /// </summary>
    public string Code { get; set; } = "ALL";

    /// <summary>
    /// When true, staff screens are locked to the deployment branch and cannot be
    /// switched in the interface. A real kiosk terminal in a real building should
    /// have this on so a member of the public cannot browse another branch's desk.
    /// </summary>
    public bool LockToBranch { get; set; }

    public bool IsNetworkWide => string.Equals(Code, "ALL", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Answers "which branch are we acting as right now" for the current request.
/// </summary>
public interface IBranchContext
{
    /// <summary>The deployment's configured branch, or null for a network-wide instance.</summary>
    Task<Branch?> GetDeploymentBranchAsync();

    /// <summary>
    /// The branch whose data the signed-in user should see: their own desk, or
    /// the deployment's branch, or null when they may see everything.
    /// </summary>
    Task<Branch?> GetCurrentBranchAsync();

    /// <summary>The branch id to stamp on a loan or return taken at this instance.</summary>
    Task<int?> GetOperatingBranchIdAsync();

    /// <summary>True when this instance may look across the whole network.</summary>
    bool IsNetworkWide { get; }
}

/// <summary>
/// Resolves the branch from, in order: the deployment setting, then the signed
/// in user's own desk. A manager with no desk on a network-wide instance sees
/// everything, which is the behaviour the reports need.
/// </summary>
public class BranchContext : IBranchContext
{
    private readonly ApplicationDbContext _context;
    private readonly BranchDeploymentOptions _options;
    private readonly IHttpContextAccessor _accessor;
    private readonly UserManager<ApplicationUser> _userManager;

    private Branch? _cached;
    private bool _resolved;

    public BranchContext(
        ApplicationDbContext context,
        IOptions<BranchDeploymentOptions> options,
        IHttpContextAccessor accessor,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _options = options.Value;
        _accessor = accessor;
        _userManager = userManager;
    }

    public bool IsNetworkWide => _options.IsNetworkWide;

    public async Task<Branch?> GetDeploymentBranchAsync()
    {
        if (_resolved)
        {
            return _cached;
        }

        _resolved = true;

        if (_options.IsNetworkWide)
        {
            return _cached = null;
        }

        var code = _options.Code.Trim().ToUpperInvariant();
        _cached = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Code.ToUpper() == code);
        return _cached;
    }

    public async Task<Branch?> GetCurrentBranchAsync()
    {
        // A locked deployment always wins: the terminal is physically at one site.
        var deployment = await GetDeploymentBranchAsync();
        if (deployment is not null && _options.LockToBranch)
        {
            return deployment;
        }

        var principal = _accessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(principal);
            if (user?.BranchId is int branchId)
            {
                return await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId);
            }
        }

        return deployment;
    }

    public async Task<int?> GetOperatingBranchIdAsync() => (await GetCurrentBranchAsync())?.Id;
}
