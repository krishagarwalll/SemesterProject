function buildButlerArgs({ buildDirAbsPath, itchUser, itchGame, channel, version }) {
  return [
    'push',
    buildDirAbsPath,
    `${itchUser}/${itchGame}:${channel}`,
    '--userversion', version,
    '--ignore', '*_BurstDebugInformation_DoNotShip/**',
    '--ignore', '*.pdb',
  ];
}

module.exports = { buildButlerArgs };
