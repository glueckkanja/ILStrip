ILStrip
========

A simple tool to strip unused types/classes from a .Net assembly. Useful when ilmerge is used with large libraries - use the /internalize switch with ilmerge and afterwards have ilstrip remove types/classes that are not referenced by publicly accessible code.

Based on mono Cecil (see [http://www.mono-project.com/Cecil](http://www.mono-project.com/Cecil) and [https://github.com/jbevain/cecil](https://github.com/jbevain/cecil))

License: [GPL](http://www.gnu.org/licenses/gpl.html "GPL")
