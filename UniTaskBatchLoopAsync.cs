namespace Batch_Loop_Async.UniTask
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;


    public static class UniTaskBatchLoopAsync
    {
        #region Common Loop

        /// <summary>
        /// Menjalankan loop batch per frame (forward/reverse) untuk menghindari freeze/frame drop di Unity.
        /// </summary>
        public static async UniTask UniTaskBatchLoop(int totalIterations, int batchSize, Action<int> process,
            bool reverse = false, PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default)
        {
            if (totalIterations <= 0)
            {
                Debug.LogWarning("Total iterations must be greater than 0");
                return;
            }

            batchSize = Mathf.Max(1, batchSize);

            if (!reverse)
            {
                for (int i = 0; i < totalIterations; i += batchSize)
                {
                    int end = Mathf.Min(i + batchSize, totalIterations);
                    for (int j = i; j < end; j++)
                        process(j);
                    await UniTask.NextFrame(timing, cancellationToken);
                }
            }
            else
            {
                for (int i = totalIterations; i > 0; i -= batchSize)
                {
                    int start = Mathf.Max(i - batchSize, 0);
                    for (int j = i - 1; j >= start; j--)
                        process(j);
                    await UniTask.NextFrame(timing, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Overload untuk memproses koleksi IReadOnlyList (List, Array, dll.).
        /// Memanggil fungsi utama UniTaskBatchLoop agar konsisten dengan pola GetAll.
        /// </summary>
        public static async UniTask UniTaskBatchLoop<T>(IReadOnlyList<T> listData, int batchSize, Action<T> process,
            bool reverse = false, PlayerLoopTiming timing = PlayerLoopTiming.Update, 
            CancellationToken cancellationToken = default)
        {
            if (listData == null)
            {
                Debug.LogWarning("List data must not be null");
                return;
            }

            await UniTaskBatchLoop(listData.Count, batchSize, (i) => process(listData[i]),
                reverse, timing, cancellationToken);
        }
        #endregion

        #region Get All

        /// <summary>
        /// Menjalankan loop batch dan mengembalikan kumpulan hasilnya ke dalam Array (Forward/Reverse).
        /// Menggunakan UniTaskBatchLoop (CommonLoop) agar konsisten dan terpusat.
        /// </summary>
        public static async UniTask<TResult[]> UniTaskBatchLoopGetAll<TResult>(
            int totalIterations,
            int batchSize,
            Func<int, TResult> process,
            bool reverse = false,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default)
        {
            var results = new List<TResult>(totalIterations);

            await UniTaskBatchLoop(totalIterations, batchSize, 
                (i) =>
                {
                    TResult result = process(i);
                    if (result != null)
                        results.Add(result);
                },
                reverse, timing, cancellationToken);

            return results.ToArray();
        }

        /// <summary>
        /// Overload untuk memproses List/Array sumber dan mengembalikan Array hasil (mendukung konversi tipe data).
        /// </summary>
        public static async UniTask<TResult[]> UniTaskBatchLoopGetAll<TSource, TResult>(
            IReadOnlyList<TSource> listData,
            int batchSize,
            Func<TSource, TResult> process,
            bool reverse = false,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default)
        {
            if (listData == null)
            {
                Debug.LogWarning("List data must not be null");
                return Array.Empty<TResult>();
            }

            return await UniTaskBatchLoopGetAll(
                listData.Count,
                batchSize,
                (i) => process(listData[i]),
                reverse,
                timing,
                cancellationToken);
        }
        #endregion

        #region Get One

        /// <summary>
        /// Mencari dan mengembalikan 1 item pertama yang tidak null (Forward/Reverse).
        /// Loop akan berhenti seketika (early return) begitu item ditemukan.
        /// </summary>
        public static async UniTask<TResult> UniTaskBatchLoopGetOne<TResult>(
            int totalIterations,
            int batchSize,
            Func<int, TResult> process,
            bool reverse = false,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default)
        {
            if (totalIterations <= 0) return default;
            batchSize = Mathf.Max(1, batchSize);

            if (!reverse)
            {
                for (int i = 0; i < totalIterations; i += batchSize)
                {
                    int end = Mathf.Min(i + batchSize, totalIterations);
                    for (int j = i; j < end; j++)
                    {
                        TResult result = process(j);
                        if (result != null)
                            return result; // Langsung berhenti seketika saat ketemu
                    }

                    await UniTask.NextFrame(timing, cancellationToken);
                }
            }
            else
            {
                for (int i = totalIterations; i > 0; i -= batchSize)
                {
                    int start = Mathf.Max(i - batchSize, 0);
                    for (int j = i - 1; j >= start; j--)
                    {
                        TResult result = process(j);
                        if (result != null)
                            return result; // Langsung berhenti seketika saat ketemu
                    }

                    await UniTask.NextFrame(timing, cancellationToken);
                }
            }

            return default;
        }

        /// <summary>
        /// Overload GetOne untuk koleksi IReadOnlyList (List, Array, dll.) dengan dukungan konversi tipe data.
        /// </summary>
        public static async UniTask<TResult> UniTaskBatchLoopGetOne<TSource, TResult>(
            IReadOnlyList<TSource> listData,
            int batchSize,
            Func<TSource, TResult> process,
            bool reverse = false,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default)
        {
            if (listData == null || listData.Count == 0) return default;

            return await UniTaskBatchLoopGetOne(
                listData.Count,
                batchSize,
                (i) => process(listData[i]),
                reverse,
                timing,
                cancellationToken);
        }

        #region Helper First / Last / Entire

        // Helper untuk mencari dari depan (First)
        public static async UniTask<TResult> UniTaskBatchLoopGetOneFirst<TResult>(
            int totalIterations, int batchSize, Func<int, TResult> process,
            PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default) =>
            await UniTaskBatchLoopGetOne(totalIterations, batchSize, process, reverse: false, timing, cancellationToken);

        public static async UniTask<TResult> UniTaskBatchLoopGetOneFirst<TSource, TResult>(
            IReadOnlyList<TSource> listData, int batchSize, Func<TSource, TResult> process,
            PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default) =>
            await UniTaskBatchLoopGetOne(listData, batchSize, process, reverse: false, timing, cancellationToken);

        // Helper untuk mencari dari belakang (Last)
        public static async UniTask<TResult> UniTaskBatchLoopGetOneLast<TResult>(
            int totalIterations, int batchSize, Func<int, TResult> process,
            PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default) =>
            await UniTaskBatchLoopGetOne(totalIterations, batchSize, process, reverse: true, timing, cancellationToken);

        public static async UniTask<TResult> UniTaskBatchLoopGetOneLast<TSource, TResult>(
            IReadOnlyList<TSource> listData, int batchSize, Func<TSource, TResult> process,
            PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default) =>
            await UniTaskBatchLoopGetOne(listData, batchSize, process, reverse: true, timing, cancellationToken);

        // Helper untuk akumulasi nilai dari seluruh item (Aggregate / Reduce)
        public static async UniTask<TResult> UniTaskBatchLoopGetOneEntire<TResult>(
            int totalIterations, int batchSize, Func<TResult, int, TResult> process,
            TResult initialValue = default, PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default)
        {
            TResult accumulator = initialValue;
            await UniTaskBatchLoop(totalIterations, batchSize, (i) =>
            {
                accumulator = process(accumulator, i);
            }, reverse: false, timing, cancellationToken);
            return accumulator;
        }

        public static async UniTask<TResult> UniTaskBatchLoopGetOneEntire<TSource, TResult>(
            IReadOnlyList<TSource> listData, int batchSize, Func<TResult, TSource, TResult> process,
            TResult initialValue = default, PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default)
        {
            if (listData == null || listData.Count == 0) return initialValue;

            return await UniTaskBatchLoopGetOneEntire(
                listData.Count,
                batchSize,
                (acc, i) => process(acc, listData[i]),
                initialValue,
                timing,
                cancellationToken);
        }

        #endregion

        /*
        // ========================== OLD IMPLEMENTATION ==========================
        public static async UniTask<T> UniTaskBatchLoopGetOne<T>(int totaliterations, int batchsize,
            Func<int, T> process, bool reverse = false)
        {
            if (!reverse)
            {
                for (int i = 0; i < totaliterations; i += batchsize)
                {
                    int end = Mathf.Min(i + batchsize, totaliterations);
                    for (int j = i; j < end; j++)
                    {
                        Debug.Log(j);
                        T result = process(j);
                        if (result != null)
                            return result;
                    }

                    await UniTask.NextFrame(); // Prevent Unity freezing
                }
            }
            else
            {
                for (int i = totaliterations; i > 0; i -= batchsize)
                {
                    int start = Mathf.Max(i - batchsize, 0);
                    for (int j = i - 1; j >= start; j--)
                    {
                        Debug.Log(j);
                        T result = process(j);
                        if (result != null)
                            return result;
                    }

                    await UniTask.Yield(); // Prevent Unity freezing
                }
            }

            return default;
        }

        public static async UniTask<T> UniTaskBatchLoopGetOneFirst<T>(int totaliterations, int batchsize,
            Func<int, T> process) => await UniTaskBatchLoopGetOne(totaliterations, batchsize, (j) => process(j));

        public static async UniTask<T>
            UniTaskBatchLoopGetOneFirst<T>(T[] arraydata, int batchsize, Func<T, T> process) =>
            await UniTaskBatchLoopGetOneFirst(arraydata.Length, batchsize, (i) => process(arraydata[i]));

        public static async UniTask<T> UniTaskBatchLoopGetOneLast<T>(int totaliterations, int batchsize,
            Func<int, T> process) => await UniTaskBatchLoopGetOne(totaliterations, batchsize, (j) => process(j), true);

        public static async UniTask<T>
            UniTaskBatchLoopGetOneLast<T>(T[] arraydata, int batchsize, Func<T, T> process) =>
            await UniTaskBatchLoopGetOneLast(arraydata.Length, batchsize, (i) => process(arraydata[i]));

        public static async UniTask<T> UniTaskBatchLoopGetOneEntire<T>(int totaliterations, int batchsize,
            Func<int, T> process)
        {
            T result = default(T);
            await UniTaskBatchLoop(totaliterations, batchsize, (j) => result = process(j));
            return result;
        }

        public static async UniTask<T> UniTaskBatchLoopGetOneEntire<T>(T[] arraydata, int batchsize,
            Func<T, T, T> process)
        {
            T result = default(T);
            await UniTaskBatchLoopGetOneEntire(arraydata.Length, batchsize, (i) =>
            {
                result = process(result, arraydata[i]);
                return result;
            });
            return result;
        }
        // ========================================================================
        */

        #endregion
    }
}