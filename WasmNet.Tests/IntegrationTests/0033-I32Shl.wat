;; invoke: shl
;; expect: (i32:168)

(module
    (func (export "shl") (result i32)
        i32.const 42
        i32.const 2
        i32.shl
    )
)