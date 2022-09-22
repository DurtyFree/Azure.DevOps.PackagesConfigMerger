# NuGet packages.config merger
The NuGet packages.config merger build task automatically merges packages.config files across a folder into one single packages.config file.
It doesn't modify or delete any of the packages.config files, it just merges the content of all found packages.config files into one.

# But why?
Due to modularity our application is split into a lot of single project solution files, which all contain their own list of referenced NuGet packages. This results in round about **385 solutions with a total of 757 packages.config files**. 
Restoring NuGet packages referenced in various different Projects / packages.config files, takes a huge amount of time just for searching & restoring each on their own. 

In our case restoring NuGet packages alone **took 20 minutes**.

So this task retrieves all packages.config located somewhere in the given root directory and merges the content into one single packages.config file. In our case this were **757 packages.config files** with a total of **6671 referenced NuGet packages**, resulting in a **unique amount of 107 packages** referenced in one single packages.config. 

The result is restoring NuGet packages takes less **than 1 minute** with the single merged packages.config now.