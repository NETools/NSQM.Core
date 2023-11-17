//// BismIllah

//using NSQM.Core.Consumer;
//using NSQM.Core.Model;
//using NSQM.Core.Producer;
//using NSQM.Data.Model.Persistence;

////var producer = new NSQMProducer("localhost:5290", Guid.Parse("3A5EF423-4F83-46E3-A0D9-20427A72B001"));
////producer.TaskFinished += TaskFinished;

////async void TaskFinished(ReceivedTask taskData, AcceptConnection connection)
////{
////	Console.WriteLine(taskData.FromId);
////	var response = await connection.Accept(NSQM.Data.Extensions.AckType.Accepted);
////	Console.WriteLine(response);
////}

////await producer.Connect(CancellationToken.None);
////Console.WriteLine("Connected.");

////var createChannelResponse = await producer.OpenChannel("TestChannel");
////Console.WriteLine(createChannelResponse);
////Console.ReadLine();

////var subscribeChannelResponse = await producer.Subscribe(createChannelResponse.Model);
////Console.WriteLine(subscribeChannelResponse);
////Console.ReadLine();

//////var createTaskResponse = await producer.PublishTask(createChannelResponse.Model, "My second task", new byte[] { 255, 255, 255 });
//////Console.WriteLine(createTaskResponse);
//////Console.ReadLine();

////await producer.Close();


//var consumer = new NSQMConsumer("localhost:5290", Guid.Parse("1A5EF423-4F83-46E3-A0D9-20427A72B001"));
//consumer.TaskReceived += TaskReceived;

//async void TaskReceived(ReceivedTask task, ResultConnection connection)
//{
//	Console.WriteLine(task.FromId);
//	var response = await connection.Done(new byte[] { 191, 192 }, NSQM.Data.Extensions.TaskStatus.TaskDone);
//	Console.WriteLine(response);
//}

//await consumer.Connect(CancellationToken.None);

//Console.WriteLine("Connected.");

//var subscribeResponse = await consumer.Subscribe("TestChannel");
//Console.WriteLine(subscribeResponse);

//Console.ReadLine();

//await consumer.Close();