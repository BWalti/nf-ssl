This repository can be temporarely used to re-produce an SSL issue I get with ESP32, while trying to download a ~5.7kB file from Azure Storage account container.

# Storage Account infos

## Available Files
The following files are available in the storage account:

![Files overview](images/ContainerFiles.png)

## TLS

Minimum TLS version of the Storage Account is currently configured to: 1.0

## Access Key

For getting files no Access key is required.
However you are not allowed to list files within the container.

# HTTP

You can also get files with plain old HTTP, which succeeds in my tests. (to ensure it is now "memory issue")

If you want to change to HTTP, adapt the `storageAccountBaseUri` within [Resource.resx](nf-ssl/Resource.resx) either manually or by using the designer:

``` xml
<data name="storageAccountBaseUri" xml:space="preserve">
  <value>https://bwaltilasertag.blob.core.windows.net/piot/</value>
</data>
```
