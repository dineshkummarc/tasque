SUBDIRS = RtmNet src data po

pkglib_DATA =  $(DLL_REFERENCES)
DLL_REFERENCES =   
EXTRA_DIST = \
	$(DLL_REFERENCES) \
	\
        intltool-extract.in     \
        intltool-merge.in       \
        intltool-update.in      \
	\
	MAINTAINERS \
	TRANSLATORS

DISTCLEANFILES =                        \
        \
        intltool-extract                \
        intltool-merge                  \
        intltool-update                 \
        po/.intltool-merge-cache

DISTCHECK_CONFIGURE_FLAGS = --disable-schemas-install --disable-scrollkeeper

# Ignore scrollkeeper issues for now.  @#*$& scrollkeeper (from Evince)
distuninstallcheck_listfiles = find . -type f -print | grep -v scrollkeeper | grep -v /share/gnome/help/ | grep -v \.omf

run: $(PROGRAM)
	cd src && ./tasque

dist-hook:
	@if test -d "$(srcdir)/.git"; \
	then \
		echo Creating ChangeLog && \
		( cd "$(top_srcdir)" && \
		  echo '# Generated by Makefile. Do not edit.'; echo; \
		  $(top_srcdir)/missing --run git log --stat ) > ChangeLog.tmp \
		&& mv -f ChangeLog.tmp $(top_distdir)/ChangeLog \
		|| ( rm -f ChangeLog.tmp ; \
		     echo Failed to generate ChangeLog >&2 ); \
	else \
		echo A git clone is required to generate a ChangeLog >&2; \
	fi
