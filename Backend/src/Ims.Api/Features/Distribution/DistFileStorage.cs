namespace Ims.Api.Features.Distribution;

internal sealed class DistFileStorage(IConfiguration configuration) : IDistStorage
{
    private string Root
    {
        get
        {
            var configured = configuration["Distribution:StorageRoot"];
            var root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "dist")
                : configured;
            Directory.CreateDirectory(root);
            return root;
        }
    }

    private string PathOf(string fileName)
    {
        if (fileName.Length == 0 || fileName != Path.GetFileName(fileName) || fileName.Contains(".."))
            throw new ArgumentException("Invalid file name.", nameof(fileName));
        return Path.Combine(Root, fileName);
    }

    public async Task SaveAsync(string fileName, byte[] bytes, CancellationToken ct)
    {
        var target = PathOf(fileName);
        var temp = target + ".tmp";
        await File.WriteAllBytesAsync(temp, bytes, ct);
        File.Move(temp, target, overwrite: true);
    }

    public Stream? OpenRead(string fileName)
    {
        var path = PathOf(fileName);
        return File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
            : null;
    }

    public void Delete(string fileName)
    {
        var path = PathOf(fileName);
        if (File.Exists(path)) File.Delete(path);
    }
}
