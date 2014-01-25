A few DLLs here are explicitly included in the solution as they are not made available via NuGet packages.

They are - with their origin: 

- UsbUirtManagedWrapper.dll - http://www.usbuirt.com/
- GetCoreTempInfoNET.dll - http://www.alcpu.com/CoreTemp/developers.html
- InputSimulator.dll - http://inputsimulator.codeplex.com/

In addition, there are a set of SpotiFire DLLs. SpotiFire DLLs are available from NuGet and https://github.com/Alxandr/SpotiFire, but (at the current time) the default ("master") branch of the DLL does not contain a required method in its API. The "future-playlist-add" branch of the GitHub project contains the required $PlaylistContainer$PlaylistList::Create(String ^name) method. So, until this API (and preferably other Playlist methods) are included in the master branch made available via NuGet, it is necessary to build the "future-playlist-add" branch manually and to explicitly copy its DLLs.

25-Jan-2014