import PST from './gen.js';

PST.registerExtension('fail', (args) => { throw new Error(args[0]); });

try {
    PST.runner();
} catch (ex) {
    console.error("FAIL!");
    console.log(ex);
}
