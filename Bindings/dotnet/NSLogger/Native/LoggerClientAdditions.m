#import <Foundation/Foundation.h>

NSString * NSLoggerGetCurrentThreadName()
{
    return NSThread.currentThread.name;
}

void NSLoggerSetCurrentThreadName(NSString *threadName)
{
    NSThread.currentThread.name = threadName;
}