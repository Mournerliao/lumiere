// swift-tools-version: 6.2

import PackageDescription

let package = Package(
  name: "LumiereMacHost",
  platforms: [.macOS(.v15)],
  products: [
    .executable(name: "LumiereMacHost", targets: ["LumiereMacHost"]),
    .library(name: "LumiereMacHostCore", targets: ["LumiereMacHostCore"]),
  ],
  targets: [
    .target(
      name: "LumiereMacHostCore",
      swiftSettings: [.swiftLanguageMode(.v5)]
    ),
    .executableTarget(
      name: "LumiereMacHost",
      dependencies: ["LumiereMacHostCore"],
      swiftSettings: [.swiftLanguageMode(.v5)]
    ),
    .testTarget(
      name: "LumiereMacHostCoreTests",
      dependencies: ["LumiereMacHostCore"],
      swiftSettings: [.swiftLanguageMode(.v5)]
    ),
  ]
)
