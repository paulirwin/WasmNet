;; invoke: gtu
;; expect: (i32:1)

(module
    (func (export "gtu") (result i32)
        i64.const 42
        i64.const 1
        i64.gt_u
    )
)