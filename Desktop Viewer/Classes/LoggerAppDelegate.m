/*
 * LoggerAppDelegate.h
 *
 * BSD license follows (http://www.opensource.org/licenses/bsd-license.php)
 * 
 * Copyright (c) 2010 Florent Pillet <fpillet@gmail.com> All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * Redistributions of  source code  must retain  the above  copyright notice,
 * this list of  conditions and the following  disclaimer. Redistributions in
 * binary  form must  reproduce  the  above copyright  notice,  this list  of
 * conditions and the following disclaimer  in the documentation and/or other
 * materials  provided with  the distribution.  Neither the  name of  Florent
 * Pillet nor the names of its contributors may be used to endorse or promote
 * products  derived  from  this  software  without  specific  prior  written
 * permission.  THIS  SOFTWARE  IS  PROVIDED BY  THE  COPYRIGHT  HOLDERS  AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT
 * NOT LIMITED TO, THE IMPLIED  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A  PARTICULAR PURPOSE  ARE DISCLAIMED.  IN  NO EVENT  SHALL THE  COPYRIGHT
 * HOLDER OR  CONTRIBUTORS BE  LIABLE FOR  ANY DIRECT,  INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY,  OR CONSEQUENTIAL DAMAGES (INCLUDING,  BUT NOT LIMITED
 * TO, PROCUREMENT  OF SUBSTITUTE GOODS  OR SERVICES;  LOSS OF USE,  DATA, OR
 * PROFITS; OR  BUSINESS INTERRUPTION)  HOWEVER CAUSED AND  ON ANY  THEORY OF
 * LIABILITY,  WHETHER  IN CONTRACT,  STRICT  LIABILITY,  OR TORT  (INCLUDING
 * NEGLIGENCE  OR OTHERWISE)  ARISING  IN ANY  WAY  OUT OF  THE  USE OF  THIS
 * SOFTWARE,   EVEN  IF   ADVISED  OF   THE  POSSIBILITY   OF  SUCH   DAMAGE.
 * 
 */
#import <Security/SecItem.h>
#import "LoggerAppDelegate.h"
#import "LoggerNativeTransport.h"
#import "LoggerWindowController.h"
#import "LoggerDocument.h"
#import "LoggerStatusWindowController.h"
#import "LoggerPrefsWindowController.h"

NSString * const kPrefPublishesBonjourService = @"publishesBonjourService";
NSString * const kPrefHasDirectTCPIPResponder = @"hasDirectTCPIPResponder";
NSString * const kPrefDirectTCPIPResponderPort = @"directTCPIPResponderPort";
NSString * const kPrefBonjourServiceName = @"bonjourServiceName";

@interface LoggerAppDelegate ()
- (BOOL)loadEncryptionCertificate:(NSError **)outError;
@end

@implementation LoggerAppDelegate
@synthesize transports, filterSets, filtersSortDescriptors, statusController;
@synthesize serverCerts;

- (id) init
{
	if ((self = [super init]) != nil)
	{
		transports = [[NSMutableArray alloc] init];

		// default filter ordering. The first sort descriptor ensures that the object with
		// uid 1 (the "Default Set" filter set or "All Logs" filter) is always on top. Other
		// items are ordered by title.
		self.filtersSortDescriptors = [NSArray arrayWithObjects:
									   [NSSortDescriptor sortDescriptorWithKey:@"uid" ascending:YES
																	comparator:
										^(id uid1, id uid2)
		{
			if ([uid1 integerValue] == 1)
				return (NSComparisonResult)NSOrderedAscending;
			if ([uid2 integerValue] == 1)
				return (NSComparisonResult)NSOrderedDescending;
			return (NSComparisonResult)NSOrderedSame;
		}],
									   [NSSortDescriptor sortDescriptorWithKey:@"title" ascending:YES],
									   nil];
		
		// resurrect filters before the app nib loads
		NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
		NSData *filterSetsData = [defaults objectForKey:@"filterSets"];
		if (filterSetsData != nil)
		{
			filterSets = [[NSKeyedUnarchiver unarchiveObjectWithData:filterSetsData] retain];
			if (![filterSets isKindOfClass:[NSMutableArray class]])
			{
				[filterSets release];
				filterSets = nil;
			}
		}
		if (filterSets == nil)
			filterSets = [[NSMutableArray alloc] init];
		if (![filterSets count])
		{
			NSMutableArray *filters = nil;

			// Try to reload pre-1.0b4 filters (will remove this code soon)
			NSData *filterData = [defaults objectForKey:@"filters"];
			if (filterData != nil)
			{
				filters = [NSKeyedUnarchiver unarchiveObjectWithData:filterData];
				if (![filters isMemberOfClass:[NSMutableArray class]])
					filters = nil;
			}
			if (filters == nil)
			{
				// Create a default set
				filters = [self defaultFilters];
			}
			NSMutableDictionary *defaultSet = [[NSMutableDictionary alloc] initWithObjectsAndKeys:
											   NSLocalizedString(@"Default Set", @""), @"title",
											   [NSNumber numberWithInteger:1], @"uid",
											   filters, @"filters",
											   nil];
			[filterSets addObject:defaultSet];
			[defaultSet release];
		}
		
		// fix for issue found by Stefan Neumärker: default filters in versions 1.0b7 were immutable,
		// leading to a crash if the user tried to edit them
		for (NSDictionary *dict in filterSets)
		{
			NSMutableArray *filters = [dict objectForKey:@"filters"];
			for (NSUInteger i = 0; i < [filters count]; i++)
			{
				if (![[filters objectAtIndex:i] isMemberOfClass:[NSMutableDictionary class]])
				{
					[filters replaceObjectAtIndex:i
									   withObject:[[[filters objectAtIndex:i] mutableCopy] autorelease]];
				}
			}
		}
	}
	return self;
}

- (void)dealloc
{
	if (serverCerts != NULL)
		CFRelease(serverCerts);
	if (serverKeychain != NULL)
		CFRelease(serverKeychain);
	[transports release];
	[super dealloc];
}

- (void)saveFiltersDefinition
{
	@try
	{
		NSData *filterSetsData = [NSKeyedArchiver archivedDataWithRootObject:filterSets];
		if (filterSetsData != nil)
		{
			[[NSUserDefaults standardUserDefaults] setObject:filterSetsData forKey:@"filterSets"];
			// remove pre-1.0b4 filters
			[[NSUserDefaults standardUserDefaults] removeObjectForKey:@"filters"];
		}
	}
	@catch (NSException * e)
	{
		NSLog(@"Catched exception while trying to archive filters: %@", e);
	}
}

- (void)prefsChangeNotification:(NSNotification *)note
{
	[self performSelector:@selector(startStopTransports) withObject:nil afterDelay:0.2];
}

- (void)startStopTransports
{
	// Start and stop transports as needed
	NSUserDefaultsController *udc = [NSUserDefaultsController sharedUserDefaultsController];
	id udcv = [udc values];
	for (LoggerTransport *transport in transports)
	{
		if ([transport isKindOfClass:[LoggerNativeTransport class]])
		{
			LoggerNativeTransport *t = (LoggerNativeTransport *)transport;
			if (t.publishBonjourService)
			{
				if ([[udcv valueForKey:kPrefPublishesBonjourService] boolValue])
					[t restart];
				else if (t.active)
					[t shutdown];
			}
			else
			{
				if ([[udcv valueForKey:kPrefHasDirectTCPIPResponder] boolValue])
					[t restart];
				else
					[t shutdown];
			}
		}
	}
}

- (void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
	// Initialize the user defaults controller
	NSUserDefaultsController *udc = [NSUserDefaultsController sharedUserDefaultsController];
	[udc setInitialValues:[NSDictionary dictionaryWithObjectsAndKeys:
						   [NSNumber numberWithBool:YES], kPrefPublishesBonjourService,
						   [NSNumber numberWithBool:NO], kPrefHasDirectTCPIPResponder,
						   [NSNumber numberWithInteger:0], kPrefDirectTCPIPResponderPort,
						   nil]];
	[udc setAppliesImmediately:NO];
	
	// Listen to prefs change notifications, where we start / stop transports on demand
	[[NSNotificationCenter defaultCenter] addObserver:self
											 selector:@selector(prefsChangeNotification:)
												 name:kPrefsChangedNotification
											   object:nil];
	// Prepare the logger status
	statusController = [[LoggerStatusWindowController alloc] initWithWindowNibName:@"LoggerStatus"];
	[statusController showWindow:self];
	[statusController appendStatus:NSLocalizedString(@"Logger starting up", @"")];

	// Retrieve server certs for SSL encryption
	NSError *certError;
	if (![self loadEncryptionCertificate:&certError])
	{
		[[NSApplication sharedApplication] performSelector:@selector(presentError:)
												withObject:certError
												afterDelay:0];
	}
	
	/* initialize all supported transports */
	
	// unencrypted Bonjour service (for backwards compatibility)
	LoggerNativeTransport *t = [[LoggerNativeTransport alloc] init];
	t.publishBonjourService = YES;
	t.secure = NO;
	[transports addObject:t];
	[t release];

	// SSL Bonjour service
	t = [[LoggerNativeTransport alloc] init];
	t.publishBonjourService = YES;
	t.secure = YES;
	[transports addObject:t];
	[t release];

	// Direct TCP/IP service (SSL mandatory)
	t = [[LoggerNativeTransport alloc] init];
	t.listenerPort = [[NSUserDefaults standardUserDefaults] integerForKey:kPrefDirectTCPIPResponderPort];
#if LOGGER_USES_SSL
	t.supportSSL = YES;
#endif
	[transports addObject:t];
	[t release];

	// start transports
	[self performSelector:@selector(startStopTransports) withObject:nil afterDelay:0];
}

- (BOOL)applicationShouldOpenUntitledFile:(NSApplication *)sender
{
	return NO;
}

- (BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)theApplication
{
	return NO;
}

- (void)applicationWillTerminate:(NSNotification *)aNotification
{
	if (serverKeychain != NULL)
	{
		SecKeychainDelete(serverKeychain);
	}
}

- (void)newConnection:(LoggerConnection *)aConnection
{
	LoggerDocument *doc = [[LoggerDocument alloc] initWithConnection:aConnection];
	[[NSDocumentController sharedDocumentController] addDocument:doc];
	[doc makeWindowControllers];
	[doc showWindows];
	[doc release];
}

- (NSMutableArray *)defaultFilters
{
	NSMutableArray *filters = [NSMutableArray arrayWithCapacity:4];
	[filters addObject:[NSDictionary dictionaryWithObjectsAndKeys:
						[NSNumber numberWithInteger:1], @"uid",
						NSLocalizedString(@"All logs", @""), @"title",
						[NSPredicate predicateWithValue:YES], @"predicate",
						nil]];
	[filters addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:
						[NSNumber numberWithInteger:2], @"uid",
						NSLocalizedString(@"Text messages", @""), @"title",
						[NSCompoundPredicate andPredicateWithSubpredicates:[NSArray arrayWithObject:[NSPredicate predicateWithFormat:@"(messageType == \"text\")"]]], @"predicate",
						nil]];
	[filters addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:
						[NSNumber numberWithInteger:3], @"uid",
						NSLocalizedString(@"Images", @""), @"title",
						[NSCompoundPredicate andPredicateWithSubpredicates:[NSArray arrayWithObject:[NSPredicate predicateWithFormat:@"(messageType == \"img\")"]]], @"predicate",
						nil]];
	[filters addObject:[NSMutableDictionary dictionaryWithObjectsAndKeys:
						[NSNumber numberWithInteger:4], @"uid",
						NSLocalizedString(@"Data blocks", @""), @"title",
						[NSCompoundPredicate andPredicateWithSubpredicates:[NSArray arrayWithObject:[NSPredicate predicateWithFormat:@"(messageType == \"data\")"]]], @"predicate",
						nil]];
	return filters;
}

- (NSNumber *)nextUniqueFilterIdentifier:(NSArray *)filters
{
	// since we're using basic NSDictionary to store filters, we add a filter
	// identifier number so that no two filters are strictly identical -- makes
	// things much easier with NSArrayController
	return [NSNumber numberWithInteger:[[filters valueForKeyPath:@"@max.uid"] integerValue] + 1];
}

- (IBAction)showPreferences:(id)sender
{
	if (prefsController == nil)
		prefsController = [[LoggerPrefsWindowController alloc] initWithWindowNibName:@"LoggerPrefs"];
	[prefsController showWindow:sender];
}

// -----------------------------------------------------------------------------
#pragma mark -
#pragma mark SSL support
// -----------------------------------------------------------------------------

- (BOOL)unlockAppKeychain
{
	if (serverKeychain != NULL)
		return (SecKeychainUnlock(serverKeychain, 0, "", true) == noErr);
	return NO;
}

- (BOOL)loadEncryptionCertificate:(NSError **)outError
{
	// Load the certificate we need to support encrypted incoming connections via SSL
	//
	// This is a tad more complicated than simply using the SSL API, because we insist
	// on using CFStreams which want certificates in a special form (linked to a keychain)
	// and we want to make this fully transparent to the user.
	//
	// To this end, we will (at each startup):
	// - generate a self-signed certificate and private key
	// - create our own private keychain
	// - setup access control to the keychain so that no dialog ever comes up
	// - import the self-signed certificate and private key into our keychain
	// - retrieve the certificate from our keychain
	// - create the required SecIdentityRef for the certificate to be recognized by the CFStream
	// - keep this in the running app and use for incoming connections
	//
	// Ideally, we would create the keychain once and open it at each startup. The drawback is that
	// the first time a connection comes in after app launch, a dialog would come up to ask for access
	// to our private keychain. I want to avoid this and make it fully transparent, hence the keychain
	// creation at each startup.
	//
	// May change this in the future.

	*outError = nil;
	
	// Path to our self-signed certificate
	NSString *tempDir = NSTemporaryDirectory();
	NSString *pemFileName = @"NSLoggerCert.pem";
	NSString *pemFilePath = [tempDir stringByAppendingPathComponent:pemFileName];
	NSFileManager *fm = [NSFileManager defaultManager];
	[fm removeItemAtPath:pemFilePath error:nil];

	// Generate a private certificate
	NSArray *args = [NSArray arrayWithObjects:
					 @"req",
					 @"-x509",
					 @"-nodes",
					 @"-days", @"3650",
					 @"-config", [[NSBundle mainBundle] pathForResource:@"NSLoggerCertReq" ofType:@"conf"],
					 @"-newkey", @"rsa:1024",
					 @"-keyout", pemFileName,
					 @"-out", pemFileName,
					 @"-batch",
					 nil];

	NSTask *certTask = [[[NSTask alloc] init] autorelease];
	[certTask setLaunchPath:@"/usr/bin/openssl"];
	[certTask setCurrentDirectoryPath:tempDir];
	[certTask setArguments:args];
    [certTask launch];
	do
	{
		[NSThread sleepUntilDate:[NSDate dateWithTimeIntervalSinceNow:0.05]];
	}
	while([certTask isRunning]);

	// Path to our private keychain
	NSString *path = [tempDir stringByAppendingPathComponent:@"NSLogger.keychain"];
	[fm removeItemAtPath:path error:nil];
	
	// Open or create our private keychain, and unlock it
	OSStatus status = -1;
	const char *keychainPath = [path fileSystemRepresentation];

	status = SecKeychainCreate(keychainPath,
							   0, "",	// fixed password (useless, really)
							   false,
							   NULL,
							   &serverKeychain);
	if (status != noErr)
	{
		// we can't support SSL without a proper keychain
		*outError = [NSError errorWithDomain:NSOSStatusErrorDomain
										code:status
									userInfo:[NSDictionary dictionaryWithObjectsAndKeys:
											  NSLocalizedString(@"NSLogger won't be able to encrypt connections", @""), NSLocalizedDescriptionKey,
											  NSLocalizedString(@"The private NSLogger keychain could not be opened or created.", @""), NSLocalizedFailureReasonErrorKey,
											  NSLocalizedString(@"Please contact the application developers", @""), NSLocalizedRecoverySuggestionErrorKey,
											  nil]];
		return NO;
	}
	[self unlockAppKeychain];

	SecCertificateRef certRef = NULL;

	// Find the certificate if we have already loaded it, or instantiate and find again
	for (int i = 0; i < 2 && status == noErr; i++)
	{
		// Search for the server certificate in the NSLogger keychain
		SecKeychainSearchRef keychainSearchRef = NULL;
		status = SecKeychainSearchCreateFromAttributes(serverKeychain, kSecCertificateItemClass, NULL, &keychainSearchRef);
		if (status == noErr)
			status = SecKeychainSearchCopyNext(keychainSearchRef, (SecKeychainItemRef *)&certRef);
		CFRelease(keychainSearchRef);
		
		// Did we find the certificate?
		if (status == noErr)
			break;

		// Load the NSLogger self-signed certificate
		NSData *certData = [NSData dataWithContentsOfFile:pemFilePath];

		// Import certificate and private key into our private keychain
		SecKeyImportExportParameters kp;
		bzero(&kp, sizeof(kp));
		kp.version = SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION;
		SecExternalFormat inputFormat = kSecFormatPEMSequence;
		SecExternalItemType itemType = kSecItemTypeAggregate;

		status = SecKeychainItemImport((CFDataRef)certData,
									   (CFStringRef)pemFileName,
									   &inputFormat,
									   &itemType,
									   0,				// flags are unused
									   &kp,				// import-export parameters
									   serverKeychain,
									   NULL);
	}

	if (status == noErr)
	{
		SecIdentityRef identityRef = NULL;
		status = SecIdentityCreateWithCertificate(serverKeychain, certRef, &identityRef);
		if (status == noErr)
		{
			CFTypeRef values[] = {
				identityRef, certRef
			};
			serverCerts = CFArrayCreate(NULL, values, 2, &kCFTypeArrayCallBacks);
			CFRelease(identityRef);
		}
	}

	if (certRef != NULL)
		CFRelease(certRef);

	// destroy the PEM file we just created, it's now in our keychain
	[fm removeItemAtPath:pemFilePath error:nil];

	if (status != noErr)
	{
		NSLog(@"Initializing encryption failed, status=%d", (int)status);
		*outError = [NSError errorWithDomain:NSOSStatusErrorDomain
										code:status
									userInfo:[NSDictionary dictionaryWithObjectsAndKeys:
											  NSLocalizedString(@"NSLogger won't be able to encrypt connections", @""), NSLocalizedDescriptionKey,
											  NSLocalizedString(@"Our private encryption certificate could not be loaded", @""), NSLocalizedFailureReasonErrorKey,
											  NSLocalizedString(@"Please contact the application developers", @""), NSLocalizedRecoverySuggestionErrorKey,
											  nil]];
		return NO;
	}
	return YES;
}

@end
