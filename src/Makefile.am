SUBDIRS = . Backends

CSC = $(GMCS)

TARGET = Tasque.exe
WRAPPER = tasque

if GTK_2_12
GTK_2_12_CSFLAGS = -define:GTK_2_12
endif

if ENABLE_DEBUG
CSFLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG $(GTK_2_12_CSFLAGS)
endif
if ENABLE_RELEASE
CSFLAGS =  -noconfig -codepage:utf8 -warn:4 $(GTK_2_12_CSFLAGS)
endif

if ENABLE_NOTIFY_SHARP
NOTIFY_SHARP_CSFLAGS = -define:ENABLE_NOTIFY_SHARP
endif

if ENABLE_BACKEND_DUMMY
DUMMY_CSFILES = \
	$(srcdir)/Backends/Dummy/*.cs
else
DUMMY_CSFILES =
endif

if ENABLE_BACKEND_RTM
RTM_CSFILES = \
	$(srcdir)/Backends/Rtm/*.cs 
else
RTM_CSFILES =
endif

if ENABLE_BACKEND_SQLITE
SQLITE_CSFILES = \
	$(srcdir)/Backends/Sqlite/*.cs
SQLITE_LIBS = -r:Mono.Data.Sqlite
else
SQLITE_CSFILES =
SQLITE_LIBS = 
endif

if ENABLE_BACKEND_EDS
EDS_CSFILES = \
       $(srcdir)/Backends/EDS/*.cs
else
EDS_CSFILES =
endif

if ENABLE_BACKEND_HIVEMINDER
HIVEMINDER_CSFILES = \
       $(srcdir)/Backends/Hiveminder/*.cs \
       $(srcdir)/Backends/Hiveminder/service/*.cs
else
HIVEMINDER_CSFILES =
endif

CSFILES = \
	$(srcdir)/AbstractTask.cs \
	$(srcdir)/AllCategory.cs \
	$(srcdir)/Application.cs \
	$(srcdir)/CellRendererDate.cs \
	$(srcdir)/CompletedTaskGroup.cs \
	$(srcdir)/DateButton.cs \
	$(srcdir)/GtkApplication.cs \
	$(srcdir)/GnomeApplication.cs \
	$(srcdir)/Logger.cs \
	$(srcdir)/NoteDialog.cs \
	$(srcdir)/NoteWidget.cs \
	$(srcdir)/Preferences.cs \
	$(srcdir)/PreferencesDialog.cs \
	$(srcdir)/RemoteControl.cs \
	$(srcdir)/RemoteControlProxy.cs \
	$(srcdir)/TaskCalendar.cs \
	$(srcdir)/TaskGroupModel.cs \
	$(srcdir)/CompletedTaskGroupModel.cs \
	$(srcdir)/TaskGroupModelFactory.cs \
	$(srcdir)/TaskGroup.cs \
	$(srcdir)/TaskWindow.cs \
	$(srcdir)/TaskTreeView.cs \
	$(srcdir)/Utilities.cs \
	$(TASQUEDLL_CSFILES) \
	\
	$(DUMMY_CSFILES) \
	\
	$(RTM_CSFILES) \
	\
	$(ICECORE_CSFILES) \
	\
	$(SQLITE_CSFILES) \
	\
	$(EDS_CSFILES) \
	\
	$(HIVEMINDER_CSFILES)

TASQUEDLL_CSFILES = \
	$(srcdir)/libtasque/DateFormatterFactory.cs \
	$(srcdir)/libtasque/Extensions.cs \
	$(srcdir)/libtasque/IBackend.cs \
	$(srcdir)/libtasque/ICategory.cs \
	$(srcdir)/libtasque/INativeApplication.cs \
	$(srcdir)/libtasque/INote.cs \
	$(srcdir)/libtasque/ITask.cs \
	$(srcdir)/libtasque/RegularExpressionFormatter.cs \
	$(srcdir)/libtasque/TaskParser.cs \
	$(srcdir)/libtasque/TaskPriority.cs \
	$(srcdir)/libtasque/TaskState.cs \
	$(srcdir)/libtasque/TranslatableToken.cs \
	$(foreach file,$(TASQUEDLL_FORMATTERS_CSFILES), $(addprefix $(srcdir)/libtasque/DateFormatters/, $(file)))

TASQUEDLL_FORMATTERS_CSFILES = \
	DateSeparatedFormatter.cs \
	DayFormatter.cs \
	DateFormatter.cs \
	IDateFormatter.cs \
	MonthFormatter.cs \
	OrdinalFormatter.cs \
	QuantityFormatter.cs \
	StringInsensitiveComparer.cs \
	TodayTomorrowFormatter.cs \
	WeekdayFormatter.cs \
	WeekFormatter.cs \
	YearFormatter.cs

RESOURCES = \
	-resource:$(top_srcdir)/data/images/tasque-16.png \
	-resource:$(top_srcdir)/data/images/tasque-22.png \
	-resource:$(top_srcdir)/data/images/tasque-24.png \
	-resource:$(top_srcdir)/data/images/tasque-32.png \
	-resource:$(top_srcdir)/data/images/tasque-48.png \
	-resource:$(top_srcdir)/data/images/note-16.png,note.png \
	-resource:$(top_srcdir)/data/images/rtmLogo.png \
	-resource:$(top_srcdir)/data/images/clock-16-0.png \
	-resource:$(top_srcdir)/data/images/clock-16-1.png \
	-resource:$(top_srcdir)/data/images/clock-16-2.png \
	-resource:$(top_srcdir)/data/images/clock-16-3.png \
	-resource:$(top_srcdir)/data/images/clock-16-4.png \
	-resource:$(top_srcdir)/data/images/clock-16-5.png \
	-resource:$(top_srcdir)/data/images/clock-16-6.png \
	-resource:$(top_srcdir)/data/images/clock-16-7.png \
	-resource:$(top_srcdir)/data/images/clock-16-8.png \
	-resource:$(top_srcdir)/data/images/clock-16-9.png \
	-resource:$(top_srcdir)/data/images/clock-16-10.png \
	-resource:$(top_srcdir)/data/images/clock-16-11.png

ASSEMBLIES =  \
	-r:System \
	-r:System.Core \
	-r:Mono.Posix \
	-r:System.Xml \
	-r:$(top_builddir)/RtmNet/RtmNet \
	$(GLIB_SHARP_20_LIBS) \
	$(GNOME_SHARP_20_LIBS) \
	$(GTK_DOTNET_20_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS) \
	$(ICE_DESKTOP_LIBS) \
	$(SQLITE_LIBS) \
	$(EVOLUTION_SHARP_LIBS)

$(TARGET): $(CSFILES) Defines.cs
	$(CSC) -unsafe -out:$@ $(CSFLAGS) $(NOTIFY_SHARP_CSFLAGS) $^ $(ASSEMBLIES) $(RESOURCES)

tasquelibdir = $(libdir)/tasque
tasquelib_DATA = $(TARGET) $(TARGET).config $(TARGET).mdb

bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(srcdir)/$(WRAPPER).in Makefile
	sed -e "s|\@prefix\@|$(prefix)|g"               \
		-e "s|\@exec_prefix\@|$(bindir)|g"			\
		-e "s|\@libdir\@|$(libdir)|g"				\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"         \
		-e "s|\@bindir\@|$(bindir)|g"               \
		-e "s|\@target\@|$(TARGET)|g"               \
		-e "s|\@wrapper\@|$(WRAPPER)|g"             \
		-e "s|\@srcdir\@|$(PWD)|g"  \
		< $< > $@
	chmod +x $(WRAPPER)

$(TARGET).mdb: $(TARGET)

$(TARGET).config: $(srcdir)/$(TARGET).config.in Makefile
	sed -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	    < $< > $@

Defines.cs: $(srcdir)/Defines.cs.in Makefile
	sed -e "s|\@version\@|$(VERSION)|"     \
		-e "s|\@datadir\@|$(datadir)|"     \
		-e "s|\@pkglibdir\@|$(pkglibdir)|" \
		< $< > $@

EXTRA_DIST = \
	$(CSFILES)					\
	$(WRAPPER).in				\
	$(TARGET).config.in		\
	$(srcdir)/Defines.cs.in		\
	$(srcdir)/tasque.pc.in		\
	$(srcdir)/OSXApplication.cs	\
	$(srcdir)/Backends/Dummy/*.cs	\
	$(srcdir)/Backends/EDS/*.cs	\
	$(srcdir)/Backends/IceCore/*.cs \
	$(srcdir)/Backends/Rtm/*.cs	\
	$(srcdir)/Backends/Sqlite/*.cs  \
	$(srcdir)/Backends/Hiveminder/*.cs \
	$(srcdir)/Backends/Hiveminder/service/*.cs \
	$(srcdir)/libtasque/*.cs \
	$(srcdir)/libtasque/DateFormatters/* \
	$(srcdir)/libtasque/libtasque.csproj

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = tasque.pc

CLEANFILES = \
	$(TARGET)					\
	$(TARGET).config	\
	$(TARGET).mdb				\
	$(WRAPPER)					\
	Defines.cs

DISTCLEANFILES =                        \
        $(WRAPPER)			\
	$(TARGET)			\
	$(TARGET).config	\
	$(TARGET).mdb		\
	Defines.cs
