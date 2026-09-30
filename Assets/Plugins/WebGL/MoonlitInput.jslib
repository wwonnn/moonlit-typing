mergeInto(LibraryManager.library, {
 MoonlitNow: function(){return performance.now();},
 MoonlitInstall: function(targetPtr){
  var target=UTF8ToString(targetPtr);
  if(window.moonlitInstalled)return;window.moonlitInstalled=true;
  window.addEventListener('keydown',function(e){
   if(e.repeat||e.ctrlKey||e.altKey||e.metaKey)return;
   if(['Escape','Enter','F2'].indexOf(e.code)>=0){SendMessage(target,'WebKey',e.code);e.preventDefault();return;}
   if(!/^Key[A-Z]$/.test(e.code))return;
   var k=e.code.slice(3).toLowerCase();if(e.shiftKey&&'reqtwop'.indexOf(k)>=0)k=k.toUpperCase();
   SendMessage(target,'WebKey',k+'|'+e.timeStamp.toFixed(3));e.preventDefault();
  },true);
  window.addEventListener('blur',function(){SendMessage(target,'WebBlur','');});
  document.addEventListener('visibilitychange',function(){if(document.hidden)SendMessage(target,'WebBlur','');});
 }
});
