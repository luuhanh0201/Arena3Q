'use strict';
exports.lobbyProfile=()=>({version:1,accountId:'guest-web-quality',accountKind:1,accountLabel:'Khách',introSeen:true,lastServerId:'s3',characters:[{
  serverId:'s3',lordHeroId:'lord_strategist',name:'Lâm Tử Yên',lordLevel:15,lordStars:5,lordExperience:65,vip:12,combatPower:1245680,
  wallet:[{id:'Banknotes',amount:12680,capacity:0},{id:'Silver',amount:5320000,capacity:0},{id:'Stamina',amount:180,capacity:180}],
  lordSkillLevels:[0,0,0,0],storySeen:true,tutorialCleared:true,heroes:[{heroId:'guardian',level:1,stars:3,shards:0}]
}]});
exports.seed=async(page,profile)=>page.evaluate(async profile=>{
  for(const info of await indexedDB.databases()){
    if(!info.name?.startsWith('/idbfs'))continue;
    const db=await new Promise((resolve,reject)=>{const request=indexedDB.open(info.name);request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(request.error)});
    try{
      if(!db.objectStoreNames.contains('FILE_DATA'))continue;
      const tx=db.transaction('FILE_DATA','readwrite'),store=tx.objectStore('FILE_DATA');
      const keys=await new Promise((resolve,reject)=>{const request=store.getAllKeys();request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(request.error)});
      const key=keys.find(key=>String(key).endsWith('/profile.json'));if(!key)continue;
      const previous=await new Promise((resolve,reject)=>{const request=store.get(key);request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(request.error)});
      store.put({...previous,timestamp:new Date(),contents:new TextEncoder().encode(JSON.stringify(profile))},key);
      await new Promise((resolve,reject)=>{tx.oncomplete=resolve;tx.onerror=()=>reject(tx.error)});
      return {database:info.name,key};
    }finally{db.close()}
  }
  throw Error('Real WebGL profile.json was not found');
},profile);
