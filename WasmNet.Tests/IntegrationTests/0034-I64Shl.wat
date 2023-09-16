;; invoke: shl
;; expect: (i64:168)

(module
    (func (export "shl") (result i64)
        i64.const 42
        i64.const 2
        i64.shl
    )
)