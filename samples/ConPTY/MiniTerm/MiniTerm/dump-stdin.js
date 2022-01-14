// Run with Node.js (https://nodejs.org)

process.stdin.setRawMode(true);

process.stdin.on('data', (chunk) => {
    console.log(JSON.stringify(chunk.toString()))
});
