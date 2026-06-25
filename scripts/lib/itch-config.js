const ITCH_USER = 'niclas-rogulski';
const ITCH_GAME = 'how-to-get-to-heaven';

const PLATFORMS = {
  windows: {
    channel: 'windows',
    buildProfile: 'Assets/Settings/Build Profiles/Windows.asset',
    buildDir: 'Build/Windows',
    exeSuffix: '.exe',
  },
  mac: {
    channel: 'mac',
    buildProfile: 'Assets/Settings/Build Profiles/MacOS.asset',
    buildDir: 'Build/MacOS',
    exeSuffix: '.app',
  },
  webgl: {
    channel: 'web',
    buildProfile: 'Assets/Settings/Build Profiles/WebGL.asset',
    buildDir: 'Build/WebGL',
    exeSuffix: null,
  },
};

module.exports = { ITCH_USER, ITCH_GAME, PLATFORMS };
