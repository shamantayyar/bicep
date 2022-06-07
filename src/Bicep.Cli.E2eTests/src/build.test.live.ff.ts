// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * Live tests for "bicep build".
 *
 * @group live/ff
 */

import {
  BicepRegistryReferenceBuilder,
  expectBrModuleStructure,
  publishModule,
} from "./utils/br";
import { invokingBicepCommand } from "./utils/command";
import {
  expectFileExists,
  pathToCachedTsModuleFile,
  pathToExampleFile,
  pathToTempFile,
  readFileSync,
  writeTempFile,
} from "./utils/fs";
import {
  fairfax,
  fairfaxEnvironmentOverrides,
} from "./utils/liveTestEnvironments";

describe("bicep build", () => {
  const testArea = "build";

  it("should fail to build with --no-restore switch if modules are not cached", () => {
    const bicep = `
module test 'br:${fairfax.registryUri}/does-not-exist:v-never' = {
  name: 'test'
}
    `;

    const bicepPath = writeTempFile("build", "no-restore.bicep", bicep);
    invokingBicepCommand("build", bicepPath, "--no-restore")
      .shouldFail()
      .withStderr(
        /.+BCP190: The module with reference "br:biceptest.+\.azurecr\..+\/does-not-exist:v-never" has not been restored..*/
      );
  });

  it("should build file with external modules", () => {
    const builder = new BicepRegistryReferenceBuilder(
      fairfax.registryUri,
      testArea
    );

    const storageRef = builder.getBicepReference("storage", "v1");
    publishModule(
      fairfaxEnvironmentOverrides,
      storageRef,
      "modules" + fairfax.suffix,
      "storage.bicep"
    );

    const passthroughRef = builder.getBicepReference("passthrough", "v1");
    publishModule(
      fairfaxEnvironmentOverrides,
      passthroughRef,
      "modules" + fairfax.suffix,
      "passthrough.bicep"
    );

    const mainContent = `
module passthrough '${passthroughRef}' = {
  name: 'passthrough'
  params: {
    text: 'hello'
    number: 42
  }
}

module localModule 'build-external-local-module.bicep' = {
  name: 'localModule'
  params: {
    passthroughResult: passthrough.outputs.result
  }
}

module webAppModuleV1 'ts/test-specs:webAppSpec-${fairfax.resourceSuffix}:1.0.0' = {
  name: 'webAppModuleV1'
}

output blobEndpoint string = localModule.outputs.blobEndpoint`;

    const localModuleContent = `
param passthroughResult string

module storage '${storageRef}' = {
  name: 'storage'
  params: {
    name: passthroughResult
  }
}

module nestedLocalModule 'build-external-nested-local-module.bicep' = {
  name: 'nestedLocalModule'
}

output blobEndpoint string = storage.outputs.blobEndpoint`;

    const nestedLocalModuleContent = `
module webAppModuleV1 'ts/test-specs:webAppSpec-${fairfax.resourceSuffix}:1.0.0' = {
  name: 'webAppModuleV1'
}`;

    const bicepPath = writeTempFile(
      "build",
      "build-external.bicep",
      mainContent
    );

    writeTempFile(
      "build",
      "build-external-local-module.bicep",
      localModuleContent
    );

    writeTempFile(
      "build",
      "build-external-nested-local-module.bicep",
      nestedLocalModuleContent
    );

    const exampleConfig = readFileSync(
      pathToExampleFile("modules" + fairfax.suffix, "bicepconfig.json")
    );
    writeTempFile("build", "bicepconfig.json", exampleConfig);

    invokingBicepCommand("build", bicepPath)
      .withEnvironmentOverrides(fairfaxEnvironmentOverrides)
      .shouldSucceed()
      .withEmptyStdout();

    expectFileExists(pathToTempFile("build", "build-external.json"));

    expectBrModuleStructure(
      builder.registry,
      "build$passthrough",
      `v1_${builder.tagSuffix}$4002000`
    );

    expectBrModuleStructure(
      builder.registry,
      "build$storage",
      `v1_${builder.tagSuffix}$4002000`
    );

    expectFileExists(
      pathToCachedTsModuleFile(
        `${fairfax.templateSpecSubscriptionId}/bicep-ci/webappspec-${fairfax.resourceSuffix}/1.0.0`,
        "main.json"
      )
    );
  });
});
