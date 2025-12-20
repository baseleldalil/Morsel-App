const { Jimp } = require('jimp');
const fs = require('fs');
const path = require('path');

async function createIcon() {
  const inputPath = path.join(__dirname, 'logo.jpeg');
  const outputDir = path.join(__dirname, 'electron', 'assets');

  // Ensure output directory exists
  if (!fs.existsSync(outputDir)) {
    fs.mkdirSync(outputDir, { recursive: true });
  }

  console.log('Reading logo.jpeg...');
  const image = await Jimp.read(inputPath);

  // Create 256x256 PNG first
  const resized = image.resize({ w: 256, h: 256 });
  const pngPath = path.join(outputDir, 'icon.png');
  await resized.write(pngPath);
  console.log('Created icon.png (256x256)');

  // Convert PNG to ICO using png-to-ico
  console.log('Converting to ICO...');
  const pngToIco = require('png-to-ico').default || require('png-to-ico');
  console.log('pngToIco type:', typeof pngToIco);
  console.log('pngToIco:', pngToIco);

  if (typeof pngToIco === 'function') {
    const icoBuffer = await pngToIco(pngPath);
    fs.writeFileSync(path.join(outputDir, 'icon.ico'), icoBuffer);
    console.log('Created icon.ico');
  } else if (pngToIco && typeof pngToIco.convert === 'function') {
    const icoBuffer = await pngToIco.convert(pngPath);
    fs.writeFileSync(path.join(outputDir, 'icon.ico'), icoBuffer);
    console.log('Created icon.ico');
  } else {
    console.log('png-to-ico module structure:', Object.keys(pngToIco || {}));
  }

  console.log('Done!');
}

createIcon().catch(console.error);
