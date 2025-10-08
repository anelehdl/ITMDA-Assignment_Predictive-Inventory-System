def split_train_test(df, split_date):
    train = df[df["date"] < split_date].copy()
    test = df[df["date"] >= split_date].copy()
    return (train, test)


def target_feature_split(df, features, target):
    X, y = df[features], df[target]
    return (X, y)
