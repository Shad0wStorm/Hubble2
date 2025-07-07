using System;
using System.Reflection;

#if DEVELOPMENT
#if ($WCMIXED?true:false$)
[assembly: AssemblyVersion("1.7.$WCREV$.2$WCMIXED?1:0$$WCMODS?1:0$")]
#else
[assembly: AssemblyVersion("1.7.$WCRANGE$.2$WCMIXED?1:0$$WCMODS?1:0$")]
#endif
#else
#if ($WCMIXED?true:false$)
[assembly: AssemblyVersion("1.7.$WCREV$.$WCMIXED?1:0$$WCMODS?1:0$")]
#else
[assembly: AssemblyVersion("1.7.$WCRANGE$.$WCMIXED?1:0$$WCMODS?1:0$")]
#endif
#endif
