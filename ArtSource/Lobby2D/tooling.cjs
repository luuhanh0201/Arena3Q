'use strict';
const path=require('node:path');
module.exports=function dependency(name){
 try{return require(name);}catch(localError){
  try{return require(path.resolve(__dirname,'../../AgentComms/codex/staging/ui-tools/node_modules',name));}
  catch{throw localError;}
 }
};
