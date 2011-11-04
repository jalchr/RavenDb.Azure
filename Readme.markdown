RavenDb.Azure
--------------

This project provides readily deployable RavenDb packages for Microsoft Windows Azure.

Rationale:

I needed a readily deployable RavenDb cluster, with the idea that per shard you have two read/write servers and a higher number of read only servers with automatic replication between them, and
in the spirit of ravendb's "it just works" philosophy this should not require any manual work besides initial configuration.

The idea is to deploy the provided package for every shard you want to use, and connect with the
sharding ravendb client to the read/write servers of every shard. ( if you don't need sharding just deploy one instance of this package and use the normal client ) 

Features:
---

* Full diagnostics support for both ravendb and azure integration.
* Automatic setup of persistent storage via clouddrive for the default and tenant databases.
* Automatic tenant database creation/deletion on all servers.
* Automatic replication setup/update.

Planned:

* Automatic index replication ( for manual indices ).
* Write server failover.

Known issues:

* Raven.Studio does not work for some reason ( need to investigate ).
* Full IIS hosting does not work because of the late entry point IStartupTask ( we would need a sooner entry point on a per request basis, this is a known Azure issue with diagnostics ).
* Do not enable Intellitrace for the roles, since that triggers the well known Newtonsoft.Json error of non verifieable generated code.
* Somestimes packaging fails because the directory "RavenDb.Azure.csx" cannot be removed. Just delete the directory yourself and repackage to resolve this issue.


Steps for deployment:
----

For both read and read/write server roles:

1. Configure the correct numbers/sizes of VM instances you want to use.
3. Configure the correct storage connection strings ( for both diagnostics and actual data storage ).
4. Configure the correct storage ammount you want to reserve ( Configuration setting "Storage.Size" ).
5. Possibly alter the preconfigured endpoints, right now the read servers are accessible load balanced on port 80 and the read/write servers on port 8080.

Deploy !