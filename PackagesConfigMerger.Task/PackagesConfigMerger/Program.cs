using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NuGet;

namespace PackagesConfigMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("RootDirectory & Result packages.config path must be given.");
            }
            string rootDirectory = args[0];
            string resultPackagesConfig = args[1];

            if (!Directory.Exists(rootDirectory))
            {
                throw new ArgumentException("Given RootDirectory does not exist.");
            }

            if (File.Exists(resultPackagesConfig))
            {
                File.Delete(resultPackagesConfig);
            }

            Console.WriteLine("Retrieving all packages.config files from Root Directory " + rootDirectory);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            List<PackageReferenceFile> packageConfigs = GetPackagesConfigFilePaths(rootDirectory);
            watch.Stop();
            Console.WriteLine($"Retrieved {packageConfigs.Count} packages.config files in {watch.Elapsed}");

            watch.Restart();
            ConcurrentBag<KeyValuePair<string, PackageReference>> packageReferences = RetrievePackageReferencesFromFiles(packageConfigs);
            watch.Stop();
            Console.WriteLine($"{packageReferences.Count} packages referenced in total (took {watch.Elapsed})");

            List<KeyValuePair<string, PackageReference>> uniquePackageReferences = packageReferences.DistinctBy(p => p.Value).ToList();
            Console.WriteLine($"{uniquePackageReferences.Count} unique packages referenced in total");

            List<KeyValuePair<string, PackageReference>> packageDuplicateReference = uniquePackageReferences.Where(p => uniquePackageReferences.Count(p2 => p2.Value.Id == p.Value.Id) > 1).OrderBy(p => p.Value.Id).ToList();
            foreach (KeyValuePair<string, PackageReference> packageReference in packageDuplicateReference)
            {
                Console.WriteLine($"Found multiple package references for package {packageReference.Value.Id} -> {packageReference.Value.Version} ({packageReference.Key}).");
            }

            PackageReferenceFile resultPackageConfig = SavePackageReferencesToFile(resultPackagesConfig, uniquePackageReferences.Select(p => p.Value).ToList());

            if (resultPackageConfig == null)
            {
                return;
            }

            Console.WriteLine($"Saved new packages.config to {resultPackageConfig.FullPath}.");
        }

        private static PackageReferenceFile SavePackageReferencesToFile(string packagesConfigFilePath, List<PackageReference> packageReferences)
        {
            if (!CreateEmptyPackagesConfig(packagesConfigFilePath))
            {
                return null;
            }

            PackageReferenceFile resultPackageConfig = new PackageReferenceFile(packagesConfigFilePath);
            packageReferences.ForEach(uniquePackageReference =>
            {
                resultPackageConfig.AddEntry(uniquePackageReference.Id, uniquePackageReference.Version, uniquePackageReference.IsDevelopmentDependency, uniquePackageReference.TargetFramework);
            });
            
            return resultPackageConfig;
        }

        private static ConcurrentBag<KeyValuePair<string, PackageReference>> RetrievePackageReferencesFromFiles(List<PackageReferenceFile> packageConfigs)
        {
            ConcurrentBag<KeyValuePair<string, PackageReference>> packageReferences = new ConcurrentBag<KeyValuePair<string, PackageReference>>();
            Parallel.ForEach(packageConfigs, packageConfig =>
            {
                Parallel.ForEach(packageConfig.GetPackageReferences(), packageReference => packageReferences.Add(new KeyValuePair<string, PackageReference>(packageConfig.FullPath, packageReference)));
            });
            return packageReferences;
        }

        private static List<PackageReferenceFile> GetPackagesConfigFilePaths(string rootDirectory)
        {
            return Directory.GetFiles(rootDirectory, "packages.config", SearchOption.AllDirectories)
                .Select(f => new PackageReferenceFile(f))
                .ToList();
        }

        private static bool CreateEmptyPackagesConfig(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("PackagesConfigMerger.EmptyPackagesTemplate.config"))
                {
                    using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        resource?.CopyTo(file);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}
